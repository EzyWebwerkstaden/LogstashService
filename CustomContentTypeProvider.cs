using Microsoft.Owin.StaticFiles.ContentTypes;

namespace LogstashService
{
    public class CustomContentTypeProvider : FileExtensionContentTypeProvider
    {
        public CustomContentTypeProvider()
        {
            Mappings.Add(".json", "application/json");
        }
    }
}