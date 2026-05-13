using System;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ConnectionStringParserTests
	{
		[Theory]
		[InlineData("host", "guest", "guest", "/", "host", 5672)]
		[InlineData("host:1234", "guest", "guest", "/", "host", 1234)]
		[InlineData("host/virtualHost", "guest", "guest", "virtualHost", "host", 5672)]
		[InlineData("host:1234/virtualHost", "guest", "guest", "virtualHost", "host", 1234)]
		[InlineData("username:password@host1,host2", "username", "password", "/", "host1", 5672)]
		[InlineData("username:password@host1,host2/virtualHost", "username", "password", "virtualHost", "host1", 5672)]
		[InlineData("username:password@host1,host2:1234", "username", "password", "/", "host1", 1234)]
		[InlineData("username:password@host1,host2:1234/virtualHost", "username", "password", "virtualHost", "host1", 1234)]
		public void Should_Parse_Connection_String_With_Various_Hosts_And_Credentials(string connectionString, string username, string password, string virtualHost, string firstHost, int port)
		{
			var config = ConnectionStringParser.Parse(connectionString);

			Assert.Equal(username, config.Username);
			Assert.Equal(password, config.Password);
			Assert.Equal(virtualHost, config.VirtualHost);
			Assert.Equal(firstHost, config.Hostnames[0]);
			Assert.Equal(port, config.Port);
		}

		[Theory]
		[InlineData("host1,host2?requestTimeout=10&publishConfirmTimeout=20&recoveryInterval=30&autoCloseConnection=false&persistentDeliveryMode=false&automaticRecovery=false&topologyRecovery=false",
			"guest", "guest", "/", "host1", "host2", 5672, false, false, false, false)]
		[InlineData("username:password@host1,host2:1234?requestTimeout=10&publishConfirmTimeout=20&recoveryInterval=30&autoCloseConnection=false&persistentDeliveryMode=false&automaticRecovery=false&topologyRecovery=false",
			"username", "password", "/", "host1", "host2", 1234, false, false, false, false)]
		[InlineData("username:password@host1,host2:1234/virtualHost?requestTimeout=10&publishConfirmTimeout=20&recoveryInterval=30&autoCloseConnection=false&persistentDeliveryMode=false&automaticRecovery=false&topologyRecovery=false",
			"username", "password", "virtualHost", "host1", "host2", 1234, false, false, false, false)]
		[InlineData("host1,host2:1234/virtualHost?requestTimeout=10&publishConfirmTimeout=20&recoveryInterval=30&autoCloseConnection=false&persistentDeliveryMode=false&automaticRecovery=false&topologyRecovery=false",
			"guest", "guest", "virtualHost", "host1", "host2", 1234, false, false, false, false)]
		[InlineData("username:password@host1,host2?requestTimeout=10&publishConfirmTimeout=20&recoveryInterval=30&autoCloseConnection=false&persistentDeliveryMode=false&automaticRecovery=false&topologyRecovery=false",
			"username", "password", "/", "host1", "host2", 5672, false, false, false, false)]
		public void Should_Parse_Multi_Host_Connection_String_With_Parameters(
			string connectionString, string username, string password, string virtualHost,
			string firstHost, string secondHost, int port,
			bool autoClose, bool persistDelivery, bool autoRecovery, bool topoRecovery)
		{
			var config = ConnectionStringParser.Parse(connectionString);

			Assert.Equal(username, config.Username);
			Assert.Equal(password, config.Password);
			Assert.Equal(virtualHost, config.VirtualHost);
			Assert.Equal(firstHost, config.Hostnames[0]);
			Assert.Equal(secondHost, config.Hostnames[1]);
			Assert.Equal(port, config.Port);
			Assert.Equal(TimeSpan.FromSeconds(10), config.RequestTimeout);
			Assert.Equal(TimeSpan.FromSeconds(20), config.PublishConfirmTimeout);
			Assert.Equal(TimeSpan.FromSeconds(30), config.RecoveryInterval);
			Assert.Equal(autoClose, config.AutoCloseConnection);
			Assert.Equal(persistDelivery, config.PersistentDeliveryMode);
			Assert.Equal(autoRecovery, config.AutomaticRecovery);
			Assert.Equal(topoRecovery, config.TopologyRecovery);
		}

		[Fact]
		public void Should_Throw_Format_Exception_When_ConnectionString_Has_Bad_Port()
		{
			const string connectionString = "username:password@host1,host2:port";

			var exception = Assert.Throws<FormatException>(() => ConnectionStringParser.Parse(connectionString));

			Assert.Equal("The supplied port 'port' in the connection string is not a number", exception.Message);
		}

		[Fact]
		public void Should_Throw_Argument_Exception_When_ConnectionString_Has_Bad_Property()
		{
			const string connectionString = "username:password@host1,host2?badproperty=true";

			var exception = Assert.Throws<ArgumentException>(() => ConnectionStringParser.Parse(connectionString));

			Assert.Equal("No configuration property named 'badproperty'", exception.Message);
		}

	}
}
