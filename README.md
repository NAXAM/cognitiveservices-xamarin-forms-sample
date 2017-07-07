OCR with Cognitive Services in Xamarin.Forms
---
A small sample on using Cognitive Services to extract text from image

# To run
In order to run, you will need to change Cognitive Service Key and Endpoint.
```c#
client = new VisionServiceClient(
                "{YOUR-API-KEY}",
                "{YOUR-ENDPOINT-URL}");
```