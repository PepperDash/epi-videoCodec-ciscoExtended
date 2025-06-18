using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using PepperDash.Core;
using Serilog.Events;

namespace epi_videoCodec_ciscoExtended.UserInterface.Utilities
    {
    public static class IconHandler
        {
        private static readonly string ProgramSlot = string.Format("program{0}", InitialParametersClass.ApplicationNumber);
        private static readonly string IconFolder = string.Format("/user/{0}/icons", ProgramSlot);
        private static readonly string OutputFile = string.Format("/user/{0}/icons/icons-base64.txt", ProgramSlot);

        public static void DumpAllPngsToBase64()
            {
            try
                {
                if (!Directory.Exists(IconFolder))
                    {
                    Directory.CreateDirectory(IconFolder);
                    Debug.LogMessage(LogEventLevel.Debug, "[IconHandler] Created icon folder: {0}", IconFolder);
                    }

                var pngFiles = Directory.GetFiles(IconFolder, "*.png");
                Debug.LogMessage(LogEventLevel.Debug, "[IconHandler] Found {0} PNG(s) in {1}", pngFiles.Length, IconFolder);

                var random = new Random();

                using (var writer = new StreamWriter(OutputFile, false))
                    {
                    foreach (var filePath in pngFiles)
                        {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        try
                            {
                            var bytes = System.IO.File.ReadAllBytes(filePath);
                            var b64 = Convert.ToBase64String(bytes);
                            var id = random.Next(10000000, 100000000).ToString(); 
                            writer.WriteLine($"{{\"fileName\":\"{fileName}\",\"iconId\":\"{id}\",\"customIconContent\":\"{b64}\"}}");
                            Debug.LogMessage(LogEventLevel.Debug, "[IconHandler] Encoded '{0}', id={1}, length={2}", fileName, id, b64.Length);
                            }
                        catch (Exception inner)
                            {
                            Debug.LogMessage(LogEventLevel.Debug, "[IconHandler] Skipping '{0}': {1}", filePath, inner.Message);
                            }
                        }
                    }

                Debug.LogMessage(LogEventLevel.Debug, "[IconHandler] Wrote base64 to: {0}", OutputFile);
                }
            catch (Exception ex)
                {
                Debug.LogMessage(LogEventLevel.Debug, "[IconHandler] DumpAllPngsToBase64 error: {0}", ex.Message);
                }
            }
        }
    }