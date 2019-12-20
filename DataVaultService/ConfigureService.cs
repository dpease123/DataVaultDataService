using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace DataVaultService
{
    internal static class ConfigureService
    {
        internal static void Configure()
        {
            HostFactory.Run(configure =>
            {
                configure.Service<Service>(service =>
                {
                    service.ConstructUsing(s => new Service());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });
                //Setup Account that window service use to run.  
                configure.RunAsLocalSystem();  //Network local system account
                configure.SetServiceName("DataVaultDataService");
                configure.SetDisplayName("DataVaultDataService");
                configure.SetDescription("Gets files from fusion to be loaded into the Data Vault.");
            });
        }
    }
}
