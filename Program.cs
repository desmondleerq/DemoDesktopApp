//using System;
//using System.IO;
using System.Diagnostics;
//using System.Threading.Tasks;
using ProofEasySDK;
using ProofEasySDK.Models;
using Newtonsoft.Json;

public class Program
{
    static Program()
    {
        // Logging Setup: Add basic logging to the console
        Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
    }

    static async Task Main(string[] args)
    {
        try
        {
            Trace.WriteLine("Starting application...");

            // Set API and Secret Key
            var apiKey = "1F880173ECD0515EC7030344A40C4619864EB98F8865699EAA223F3B11C99F5E";
            var secretKey = "RUe91ep53AhCVmm6w1BF1a/aFRVGA4OFb3PPrp3MdQpBO6yr6uxe8AmmJiCNIr8xEmU=";
            var sdk = new ProofEasySDK.ProofEasyPROC(apiKey, secretKey);

            // Generate and Upload QR Code only
            Trace.WriteLine("Generating and Uploading QR Code...");
            var generateUniqueID = await sdk.GenerateUniqueIDAsync();
            var uniqueId = generateUniqueID.uniqueId;
            var qrImagePath = generateUniqueID.qrImagePath; //Return QR Code for placing to physical document if applicable
            Trace.WriteLine("QR Code has been generated and uploaded successfully.");

            // Compute Document Hash
            Trace.WriteLine("Computing document hash...");
            var originalDocumentPath = "/Users/desmondlee/Dev/dotnet/Invoice.pdf";
            var originalDocumentHash = sdk.ComputeHash(originalDocumentPath);
            Trace.WriteLine("Document hash computed successfully.");

            // Submit Document 
            Trace.WriteLine("Submitting document...");
            var documentSubmitRequest = new DocumentSubmitRequest
            {
                uniqueId = uniqueId,
                fileurl = qrImagePath, // Only submit QR Code image
                isfileurlpublic = 1.ToString(),
                metadata = "Name: Name1 || Title: Title1 || Email: email1@test.com",
                parent_delimiter = "||",
                child_delimiter = ":",
                Ispublic = 1.ToString(),
                authorizedusers = "",
                Redirecturl = "",
                isredirecturlprivate = 0.ToString(),
                tokenactiveduration = 120.ToString(),
                //IsVerificationGatewayRequired = "false", //for Mainnet only
                sendmetadatatoblockchain = "true",
                metadataforblockchain = originalDocumentHash, // Original document hash || ...
                isparent = 1.ToString(),
                parentid = ""
            };

            var documentSubmit = await sdk.SubmitDocumentAsync(documentSubmitRequest);
            // Serialize the blockchainStatus object to a JSON string
            string documentSubmitJson = JsonConvert.SerializeObject(documentSubmit, Formatting.Indented);
            Trace.WriteLine("Document submitted successfully: " + documentSubmitJson);

            // Introduce a delay of 5 seconds
            await Task.Delay(5000);
            
            // Get Blockchain Status
            Trace.WriteLine("Getting blockchain status...");
            string valueDocumentBlockchain = "";
            var blockchainStatus = await sdk.GetBlockchainStatusAsync(uniqueId);
            // Serialize the blockchainStatus object to a JSON string
            string blockchainStatusJson = JsonConvert.SerializeObject(blockchainStatus, Formatting.Indented);
            Trace.WriteLine("Blockchain status retrieved successfully: " + blockchainStatusJson);

            // Verify Document Hash
            Trace.WriteLine("Verifying document hash...");
            // Check if the blockchaindata property is not null
            if (blockchainStatus != null && blockchainStatus.blockchaindata != null)
            {
                // Split the blockchainData string into substrings based on the "||" delimiter
                string[] parts = blockchainStatus.blockchaindata.Split(new string[] { "||" }, StringSplitOptions.None);
                // Check if there are at least two parts (to ensure there is a value after "||")
                if (parts.Length >= 2)
                {
                    // Extract the value after "||"
                    valueDocumentBlockchain = parts[1]; //refers to document hash position in metadataforblockchain 
                    Trace.WriteLine("Document Blockchain Value: " + valueDocumentBlockchain);
                }
                else
                {
                    Trace.WriteLine("Document Blockchain Value not found.");
                }
            }
            else
            {
                Trace.WriteLine("Blockchain data is null or empty.");
            }
            var isVerified = sdk.VerifyDocumentHash(valueDocumentBlockchain, originalDocumentHash);
            Trace.WriteLine(isVerified ? "Document verified successfully." : "Document verification failed.");
        }
        catch (Exception ex)
        {
            // Catch and log exceptions
            Trace.WriteLine($"An error occurred: {ex.Message}");
            // You can add more detailed logging here if needed
        }
    }
}