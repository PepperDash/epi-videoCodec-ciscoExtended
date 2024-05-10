//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace epi_videoCodec_ciscoExtended.UiExtensions
//{
//	 //MAKE ICONS BEFORE OTHER EXTENSIONS THAT HAVE ICONID REF TO CUSTOM ICON. UPLOAD FIRST TO DEFINE IDS
//    public class IconHandler: IUiExtensionHandler
//    {
//        //chat gpt on how to convert icon file to base64 for cisco command. upload of custom icon requirement

//        public IconHandler() { }
//        public IconHandler(string filePath)
//        {

//            try
//            {
//                // Read the file into a byte array
//                byte[] fileBytes = File.ReadAllBytes(filePath);

//                // Convert the byte array to a Base64 string
//                string base64String = Convert.ToBase64String(fileBytes);

//                // Output the base64 string
//                Console.WriteLine("Base64 Encoded String:");
//                Console.WriteLine(base64String);

//                // Now you can use this base64String as part of your command to send to the Cisco Room OS device
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine("An error occurred: " + e.Message);
//            }
//        }
//    }
//}
