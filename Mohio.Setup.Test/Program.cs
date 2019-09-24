using System;
using System.Diagnostics;

namespace Mohio.Setup.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Installer.Instance.Start(new ProcessStartInfo());
            }
            catch
            {
                //Ignor
            }
        }
    }
}
