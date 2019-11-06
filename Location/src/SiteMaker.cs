using System;
using System.Reflection;
using RestSharp;
using RestSharp.Extensions;
using ImageMagick;
using Elements.Geometry;
using Elements;
using System.IO;

namespace Location
{
    public class SiteMaker
    {
        private const double _earthRadius = 6378137;
        private const double _originShift = 2 * Math.PI * _earthRadius / 2;
        private const double _webMercMax = 20037508.342789244;

        internal static Elements.Topography CreateTopographyFromMapbox(double latitude, 
                                                               double longitude, 
                                                               string mapboxUri, 
                                                               string mapboxToken, 
                                                               Vector3 offset,
                                                               int x,
                                                               int y,
                                                               int imageSize,
                                                               string imagePath)
        {
            // Get map tiles from Mapbox
            var client = new RestSharp.RestClient(mapboxUri);
            var zoom = 16;
            
            var req = new RestRequest($"v4/mapbox.terrain-rgb/{zoom}/{x}/{y}@2x.pngraw");
            req.AddQueryParameter("access_token", mapboxToken);
            var response = client.DownloadData(req);
            double[] elevationData;

            // Sampling at 512x512 yields a mesh
            // too large to display with ushort indices.
            // It's also just too much data. 
            var sampleSize = 4;
            using (var image = new MagickImage(response))
            {
                image.Format = MagickFormat.Png;
                elevationData = GetElevationData(image, imageSize, imageSize, sampleSize);
            };
            var tileSize = GetTileSizeMeters(zoom);
            var cellSize = tileSize / ((imageSize / sampleSize) - 1);
            var colorizer = new Func<Triangle, Elements.Geometry.Color>((t) => {
                return Colors.White;
            });
            var origin = TileIdToCenterWebMercator(x, y, zoom) - new Vector3(tileSize / 2, tileSize / 2) - offset;
            var topoMaterial = new Material($"Topo_{Guid.NewGuid().ToString()}", Colors.White, 0.5f, 0.0f, imagePath);
            var topo = new Topography(origin, 
                                    cellSize, 
                                    cellSize, 
                                    elevationData, 
                                    (imageSize / sampleSize) - 1, 
                                    colorizer,
                                    topoMaterial);
            return topo;
        }

        internal static string GetMapImage(double latitude, double longitude, int x, int y, string mapboxUri, string mapboxToken, int imageSize)
        {
            var client = new RestSharp.RestClient(mapboxUri);
            var zoom = 16;
            var styleId = "mapbox/streets-v11";
            // var styleId = "ikeough/cjy4r0l2h00ig1dmuk7zeximh";
            var imageTileUrl = $"styles/v1/{styleId}/tiles/{imageSize}/{zoom}/{x}/{y}@2x";
            var req = new RestRequest(imageTileUrl);
            req.AddQueryParameter("access_token", mapboxToken);
            var tmpImagePath = Path.Combine(Path.GetTempPath(), $"Texture_{Guid.NewGuid().ToString()}.jpg");
            client.DownloadData(req).SaveAs(tmpImagePath);
            return tmpImagePath;
        }

        private static double[] GetElevationData(MagickImage image, int imageWidth, int imageHeight, int step)
        {
            var result = new double[imageWidth / step * imageHeight / step];
            var i = 0;
            var pc = image.GetPixels();

            // Rows first
            for (var y = image.Height - 1; y >= 0; y -= step)
            {
                // Then columns
                // Columns are read last->first,
                // because Image Magick's coordinate system
                // has its origin in the "upper left".
                for (var x = 0; x < image.Width; x += step)
                {
                    var c = pc.GetPixel(x, y).ToColor();
                    var height = -10000 + ((c.R * 256 * 256 + c.G * 256 + c.B) * 0.1);
                    result[i] = height;
                    i++;
                }
            }
            return result;
        }

        internal static double GetTileSizeMeters(int zoom)
        {
            // Circumference of the earth divided by 2^zoom
            // return (2* Math.PI * EarthRadius)/ Math.pow(2,zoom)
            return 40075016.685578 / Math.Pow(2, zoom);
        }

        internal static Vector3 LatLonToMeters(double lat, double lon)
        {
            var posx = lon * _originShift / 180.0;
            var posy = Math.Log(Math.Tan((90.0 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0);
            posy = posy * _originShift / 180.0;
            return new Vector3(posx, posy);
        }

        internal static int Long2Tile(double lon, int zoom)
        {
            return (int)Math.Floor((lon + 180) / 360 * Math.Pow(2, zoom));
        }

        internal static int Lat2Tile(double lat, int zoom)
        {
            return (int)Math.Floor((1 - Math.Log(Math.Tan(lat * Math.PI / 180) + 1 / Math.Cos(lat * Math.PI / 180)) / Math.PI) / 2 * Math.Pow(2, zoom));
        }

        internal static Vector3 TileIdToCenterWebMercator(int x, int y, int zoom)
        {
            var tileCnt = Math.Pow(2, zoom);
            var centerX = x + 0.5;
            var centerY = y + 0.5;

            centerX = ((centerX / tileCnt * 2) - 1) * _webMercMax;
            centerY = (1 - (centerY / tileCnt * 2)) * _webMercMax;
            return new Vector3(centerX, centerY);
        }
    }
}
