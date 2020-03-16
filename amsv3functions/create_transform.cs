//
// Azure Media Services REST API v3 - Functions
//
// create_transform - This function creates Transform in AMS account.
//
//  Input:
//      {
//          "transformName":  "Name of the Transform",
//          "builtInStandardEncoderPreset":
//          {
//              "presetName": "string"  // string (default: AdaptiveStreaming)
//          }
//          "videoAnalyzerPreset":
//          {
//              "audioInsightsOnly": true|false,    // boolean: Whether to only extract audio insights when processing a video file
//              "audioLanguage": "en-US"           // string: The language for the audio payload in the input using the BCP-47 format of 'language tag-region' (e.g: 'en-US').
//
//              // The list of supported languages are:
//              // 'en-US', 'en-GB', 'es-ES', 'es-MX', 'fr-FR', 'it-IT', 'ja-JP', 'pt-BR', 'zh-CN'.
//          }
//      }
//  Output:
//      {
//          "transformId":  "Id of the Transform"
//      }
//

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;

using Newtonsoft.Json;
using System;

namespace amsv3functions
{
    public static class create_transform
    {
        private static Dictionary<string, EncoderNamedPreset> encoderPreset = new Dictionary<string, EncoderNamedPreset>()
        {
            { "AdaptiveStreaming", EncoderNamedPreset.AdaptiveStreaming },
            { "H264MultipleBitrate1080p", EncoderNamedPreset.H264MultipleBitrate1080p },
            { "H264MultipleBitrate720p", EncoderNamedPreset.H264MultipleBitrate720p },
            { "H264MultipleBitrateSD", EncoderNamedPreset.H264MultipleBitrateSD },
            { "AACGoodQualityAudio", EncoderNamedPreset.AACGoodQualityAudio }
        };

        [FunctionName("create_transform")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - create_transform was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            // Validate input objects
            if (data.transformName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass transformName in the input object" });
            if (data.builtInStandardEncoderPreset == null && data.videoAnalyzerPreset == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass preset in the input object" });
            string transformName = data.transformName;

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            string transformId = null;

            try
            {
                IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);
                // Does a Transform already exist with the desired name? Assume that an existing Transform with the desired name
                // also uses the same recipe or Preset for processing content.
                Transform transform = client.Transforms.Get(amsconfig.ResourceGroup, amsconfig.AccountName, transformName);

                if (transform == null)
                {
                    // Create a new Transform Outputs array - this defines the set of outputs for the Transform
                    TransformOutput[] outputs = new TransformOutput[]
                    {
			            new TransformOutput(
                            new StandardEncoderPreset(
                                codecs: new Codec[]
                                {
							        // Add an AAC Audio layer for the audio encoding
							        new AacAudio(
                                        channels: 1,
                                        samplingRate: 48000,
                                        bitrate: 64000,
                                        profile: AacAudioProfile.AacLc
                                    ),
							        // Next, add a H264Video for the video encoding
						            new H264Video (
								        // Set the GOP interval to 2 seconds for both H264Layers
								        keyFrameInterval:TimeSpan.FromSeconds(2),
                                        stretchMode: StretchMode.None,
								            // Add H264Layers, one at HD and the other at SD. Assign a label that you can use for the output filename
								        layers:  new H264Layer[]
                                        {
                                            new H264Layer (
                                                    bitrate: 1000000,
                                                    maxBitrate: 1000000,
                                                    label: "HD",
                                                    bufferWindow: TimeSpan.FromSeconds(5),
                                                    width: "1080",
                                                    height: "720",
                                                    referenceFrames: 3,
                                                    entropyMode: "Cabac",
                                                    adaptiveBFrame: true,
                                                    frameRate: "0/1"
                                            ),
                                            new H264Layer (
                                                    bitrate: 750000,
                                                    maxBitrate: 750000,
                                                    label: "SD",
                                                    bufferWindow: TimeSpan.FromSeconds(5),
                                                    width: "720",
                                                    height: "480",
                                                    referenceFrames: 3,
                                                    entropyMode: "Cabac",
                                                    adaptiveBFrame: true,
                                                    frameRate: "0/1"
                                            ),
                                            new H264Layer (
                                                    bitrate: 500000,
                                                    maxBitrate: 500000,
                                                    label: "HD",
                                                    bufferWindow: TimeSpan.FromSeconds(5),
                                                    width: "540",
                                                    height: "360",
                                                    referenceFrames: 3,
                                                    entropyMode: "Cabac",
                                                    adaptiveBFrame: true,
                                                    frameRate: "0/1"
                                            ),
                                            new H264Layer (
                                                    bitrate: 200000,
                                                    maxBitrate: 200000,
                                                    label: "HD",
                                                    bufferWindow: TimeSpan.FromSeconds(5),
                                                    width: "360",
                                                    height: "240",
                                                    referenceFrames: 3,
                                                    entropyMode: "Cabac",
                                                    adaptiveBFrame: true,
                                                    frameRate: "0/1"
                                            )
                                        }
                                    ),
                                },
						        // Specify the format for the output files - one for video+audio, and another for the thumbnails
						        formats: new Format[]
                                {
							        // Mux the H.264 video and AAC audio into MP4 files, using basename, label, bitrate and extension macros
							        // Note that since you have multiple H264Layers defined above, you have to use a macro that produces unique names per H264Layer
							        // Either {Label} or {Bitrate} should suffice
					 
							        new Mp4Format(
                                        filenamePattern:"Video-{Basename}-{Label}-{Bitrate}{Extension}"
                                    )
							        //,new PngFormat(
							        //    filenamePattern:"Thumbnail-{Basename}-{Index}{Extension}"
							        //)
						        }
                            ),
                            onError: OnErrorType.StopProcessingJob,
                            relativePriority: Priority.Normal
                        )
                    };

                    string description = "A simple custom encoding transform with 2 MP4 bitrates";
                    // Create the custom Transform with the outputs defined above
                    transform = client.Transforms.CreateOrUpdate(amsconfig.ResourceGroup, amsconfig.AccountName, transformName, outputs, description);                    

                    transformId = transform.Id;
                }
            }
            catch (ApiErrorException e)
            {
                log.Info($"ERROR: AMS API call failed with error code: {e.Body.Error.Code} and message: {e.Body.Error.Message}");
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "AMS API call error: " + e.Message
                });
            }


            return req.CreateResponse(HttpStatusCode.OK, new
            {
                transformId = transformId
            });
        }

        private static IAzureMediaServicesClient CreateMediaServicesClient(MediaServicesConfigWrapper config)
        {
            ArmClientCredentials credentials = new ArmClientCredentials(config.serviceClientCredentialsConfig);

            return new AzureMediaServicesClient(config.serviceClientCredentialsConfig.ArmEndpoint, credentials)
            {
                SubscriptionId = config.SubscriptionId,
            };
        }
    }
}
