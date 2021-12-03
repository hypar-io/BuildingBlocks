
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Hypar.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await HyparServer.StartAsync(
                args,
                Path.GetFullPath(Path.Combine(@"/Users/ikeough/Documents/Hypar/BuildingBlocks/Structure/StructureByEnvelope/server", "..")),
                typeof(Structure.Function),
                typeof(Structure.StructureInputs));
        }
    }
}