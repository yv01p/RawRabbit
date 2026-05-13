using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class NamingConventionsTests
	{
		[Theory]
		[InlineData(@"\""Services\\Micro.Services.MagicMaker\\bin\\Micro.Services.MagicMaker.exe\"" ", "micro_services_magicmaker")]
		[InlineData(@"\""Services\\Micro.Services.MagicMaker\\bin\\Micro.Services.MagicMaker.vshost.exe\"" ", "micro_services_magicmaker")]
		[InlineData(@"""c:\\windows\\system32\\inetsrv\\w3wp.exe -ap \""Application.Name\"" -v \""v4.0\"" -l \""webengine4.dll\"" -a \\\\.\\pipe\\iisipm6866bb0f-a36a-49b2-9ea8-d83ca69e873d -w \""\"" -m 0 -t 20 -ta 0""", "application_name")]
		[InlineData(@"""c:\\windows\\system32\\inetsrv\\w3wp.exe -ap \""Application.Name\"" -v \""v4.0\"" -l \""webengine4.dll\"" -a \\\\.\\pipe\\iisipm6866bb0f-a36a-49b2-9ea8-d83ca69e873d -h \""C:\\inetpub\\temp\\apppools\\voyager_dk\\voyager_dk.config\"" -w \""\"" -m 0 -t 20 -ta 0""", "application_name")]
		public void Should_Get_Application_Name_From_Single_Command_Line(string commandLine, string expectedName)
		{
			var actual = NamingConventions.GetApplicationName(commandLine);

			Assert.Equal(expectedName, actual);
		}

		[Fact]
		public void Should_Be_Able_To_Get_Appllication_Name_From_Dot_Net_Core_Hosted_Apps()
		{
			var commandLime = new[]
			{
				"C:\\PathToNuget\\packages\\dotnet-test-xunit\\2.2.0-preview2-build1029\\lib\\netcoreapp1.0\\dotnet-test-xunit.dll",
				"C:\\ProjectPath\\RawRabbit\\test\\RawRabbit.IntegrationTests\\bin\\Debug\\netcoreapp1.0\\RawRabbit.IntegrationTests.dll",
				"--designtime",
				"--port",
				"55821",
				"--wait-command"
			};

			var appName = NamingConventions.GetApplicationName(commandLime);

			Assert.Equal("rawrabbit_integrationtests", appName);
		}
	}
}
