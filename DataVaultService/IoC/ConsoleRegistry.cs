using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructureMap;
using SVGLeasePlanService.Data;

namespace SVGLeasePlanService.IoC
{
    public class ConsoleRegistry : Registry
    {
        public ConsoleRegistry()
        {
            Scan(scan =>
            {
                scan.TheCallingAssembly();
                scan.WithDefaultConventions();
            });


            For<LdbContext>().Use(context => CreateNewContext(context));
            For<ISVGWorker>().Use<SVGWorker>().Ctor<ILdbRepository>().Is(c=> c.GetInstance<ILdbRepository>());
                

           

        }

        private LdbContext CreateNewContext(IContext context)
        {
            var myContext = new LdbContext();
            myContext.Configuration.ProxyCreationEnabled = false;
            return myContext;
        }
    }
}

