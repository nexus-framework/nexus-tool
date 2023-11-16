using System.Drawing;

namespace Nexus;

public static class Constants
{ 
    public const string NexusLogo = @"
    _   __                                                 
   / | / /__  _  ____  _______                             
  /  |/ / _ \| |/_/ / / / ___/                             
 / /|  /  __/>  </ /_/ (__  )                              
/_/ |_/\___/_/|_|\__,_/____/                           __  
   / ____/________ _____ ___  ___ _      ______  _____/ /__
  / /_  / ___/ __ `/ __ `__ \/ _ \ | /| / / __ \/ ___/ //_/
 / __/ / /  / /_/ / / / / / /  __/ |/ |/ / /_/ / /  / ,<   
/_/   /_/   \__,_/_/ /_/ /_/\___/|__/|__/\____/_/  /_/|_|  
                                                           
";
    public static class Colors
    {
        public static Color Default = Color.Black;
        public static Color Info = Color.FromArgb(145, 203, 215); 
        public static Color Error = Color.FromArgb(237, 101, 113);     
        public static Color Success = Color.FromArgb(255, 208, 134);
    }
}