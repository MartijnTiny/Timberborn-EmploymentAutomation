using System;
using Timberborn.ModManagerScene;

namespace EmploymentAutomation.Configuration
{
    public class Plugin : IModStarter
    {
        public void StartMod(IModEnvironment modEnvironment)
        {
            const string modName = "Employment Automation";
            Console.WriteLine("Hello " + modName + "!");
        }
    }
}