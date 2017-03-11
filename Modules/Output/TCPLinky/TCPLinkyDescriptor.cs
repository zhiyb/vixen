using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vixen.Module;
using Vixen.Module.Controller;
using System.Net;

namespace VixenModules.Output.TCPLinky
{
    internal class TCPLinkyDescriptor : ControllerModuleDescriptorBase
    {
        private Guid _typeId = new Guid("{9BC92813-7CD4-44F8-82D6-5FF1F028FE78}");

        public override string Author
        {
            get { return "Yubo Zhi"; }
        }

        public override string Description
        {
            get { return "TCP-Linky controller"; }
        }

        public override Type ModuleClass
        {
            get { return typeof(TCPLinky); }
        }

        public override Type ModuleDataClass
        {
            get { return typeof(TCPLinkyData); }
        }

        public override Guid TypeId
        {
            get { return _typeId; }
        }

        public override string TypeName
        {
            get { return "TCPLinky"; }
        }

        public override string Version
        {
            get { return "1.0"; }
        }

        public override int UpdateInterval
        {
            get
            {
                return 20;
            }
        }
    }
}