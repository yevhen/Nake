using System.Collections.Generic;

using Cake.Core;
using Cake.Core.Configuration;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Nake.Bake
{
    class CakeContextBuilder
    {
        public static ICakeContext Build()
        {
            var log = new CakeBuildLog(new CakeConsole());
            var environment = new CakeEnvironment(new CakePlatform(), new CakeRuntime(), log);
            var fileSystem = new FileSystem();
            var globber = new Globber(fileSystem, environment);
            
            var arguments = (ICakeArguments) null;
            var dataService = (ICakeDataService) null; // new CakeDataService() - internal
            
            var configValues = new Dictionary<string, string>(); // get from disk
            var configuration = new CakeConfiguration(configValues);

            var toolResolutionStrategy = new ToolResolutionStrategy(fileSystem, environment, globber, configuration);
            var toolRepository = new ToolRepository(environment);
            var toolLocator = new ToolLocator(environment, toolRepository, toolResolutionStrategy);
            var processRunner = new ProcessRunner(fileSystem, environment, log, toolLocator, configuration);

            var cake = new CakeContext(
                fileSystem,
                environment,
                globber,
                log,
                arguments,
                processRunner,
                new WindowsRegistry(),
                toolLocator,
                dataService,
                configuration
            );

            return cake;
        }
    }
}