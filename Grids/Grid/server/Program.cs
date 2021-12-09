
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
                Path.GetFullPath(Path.Combine(@"/Users/serenali/Github/hypar/BuildingBlocks/Grids/Grid/server", "..")),
                typeof(Grid.Function),
                typeof(Grid.GridInputs));
        }
    }
}