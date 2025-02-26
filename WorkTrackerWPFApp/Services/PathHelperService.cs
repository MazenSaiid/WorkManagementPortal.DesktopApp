using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTrackerWPFApp.Services
{
    public static class PathHelperService
    {
        public static string GetBaseDirectoryLogFilePath(IConfiguration configuration)
        {
            // Replace placeholder with the actual username
            string username = Environment.UserName;
            string baseDirTemplate = configuration["SerilogLogging:BaseDirectory"];
            string baseDir = baseDirTemplate.Replace("{username}", username);

            // Ensure the directory exists
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            // Return the full log file path
            return baseDir;
        }
    }
}
