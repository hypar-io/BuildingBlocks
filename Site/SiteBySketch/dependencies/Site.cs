using Newtonsoft.Json;
namespace Elements
{
    public partial class Site
    {
        // Used by add overrides to distinguish new sites
        [JsonProperty("Add Id")]
        public string AddId { get; set; }
    }
}