using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RedmineRipper.Ripper;

/*
 *  by Alsch092 @ github
 *  feel free to make improvements, fork, or raise issues.
 *  */

namespace RedmineRipper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Usage: ./RedmineRipper.exe <base_url> <username> <password> <export_format>");
            Console.WriteLine("Example: ./RedmineRipper.exe http://redmine-site.org/ account password .pdf");

            if(args.Length >= 4)
            {
                string base_url = args[0];
                string user = args[1];
                string pass = args[2];
                string export_format = args[3];

                Ripper redmineRipper = new Ripper(base_url);

                if (export_format == ".pdf")
                {
                    redmineRipper.Orchestrate(user, pass, 0, 1000, true, ExportTypes.pdf, true);
                }
                else if (export_format == ".atom")
                {
                    redmineRipper.Orchestrate(user, pass, 0, 1000, true, ExportTypes.atom, true);
                }
                else if (export_format == ".html")
                {
                    redmineRipper.Orchestrate(user, pass, 0, 1000, true, ExportTypes.html, true);
                }
            }
            else
            {
                Ripper redmineRipper = new Ripper("http://mysite.com/"); //change this to the site you want
                redmineRipper.Orchestrate("account", "password", 0, 100, false, ExportTypes.pdf, true); //start at issue 0 and iterate until issue 100
            }

            Console.WriteLine("Program is finished, press any key to exit.");
            Console.ReadLine();
        }
    }
}
