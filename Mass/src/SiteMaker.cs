using System;
using System.Collections.Generic;
using System.Linq;
using RestSharp;
using ImageMagick;
using Elements.GeoJSON;
using Elements.Geometry;

namespace Mass
{
    public class SiteMaker
    {
        private const string _mapboxToken = "pk.eyJ1IjoiaWtlb3VnaCIsImEiOiJjamc4ZzFucnoxdmQ0MnBxZDY5dW8za3c4In0.fQy46phspOdUtJleHvrqlg";
        private const string _mapboxBaseUri = "https://api.mapbox.com/v4";
        private const double _earthRadius = 6378137;
        private const double _originShift = 2 * Math.PI * _earthRadius / 2;
        private const double _webMercMax = 20037508.342789244;

        public SiteMaker(Feature[] location)
        {
            if (!(location[0].Geometry is Elements.GeoJSON.Polygon boundary))
            {
                throw new ArgumentException("Site Boundary is not a Polygon.");
            }
            var origin = boundary.Coordinates[0][0];
            var offset = LatLonToMeters(origin.Latitude, origin.Longitude);
            var boundaryCoords = boundary.Coordinates[0].ToList().Distinct();
            var verts = new List<Vector3>();
            foreach (var vertex in boundaryCoords)
            {
                verts.Add(LatLonToMeters(vertex.Latitude, vertex.Longitude) - offset);
            }
            verts.Reverse();
            Boundary = new Elements.Geometry.Polygon(verts.ToArray());
            Topography = CreateTopographyFromMapbox(origin.Latitude,
                                                    origin.Longitude,
                                                    _mapboxBaseUri,
                                                    _mapboxToken,
                                                    offset);
        }

        private Elements.Topography CreateTopographyFromMapbox(double latitude, 
                                                               double longitude, 
                                                               string mapboxUri, 
                                                               string mapboxToken, 
                                                               Vector3 offset)
        {
            // Get map tiles from Mapbox
            var client = new RestClient(mapboxUri);
            var zoom = 16;
            var x = Long2Tile(longitude, zoom);
            var y = Lat2Tile(latitude, zoom);
            var req = new RestRequest($"mapbox.terrain-rgb/{zoom}/{x}/{y}@2x.pngraw");
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
                elevationData = GetElevationData(image, 512, 512, sampleSize);
            };
            var tileSize = GetTileSizeMeters(zoom);
            var cellSize = tileSize / ((512 / sampleSize) - 1);
            var colorizer = new Func<Triangle, Color>((t) => {
                return Colors.Brown;
            });
            var origin = TileIdToCenterWebMercator(x, y, zoom) - new Vector3(tileSize / 2, tileSize / 2) - offset;
            var topo = new Elements.Topography(origin, 
                                               cellSize, 
                                               cellSize, 
                                               elevationData, 
                                               (512 / sampleSize) - 1, 
                                               colorizer);
            return topo;
        }

        private double[] GetElevationData(MagickImage image, int imageWidth, int imageHeight, int step)
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

        private double GetTileSizeMeters(int zoom)
        {
            // Circumference of the earth divided by 2^zoom
            // return (2* Math.PI * EarthRadius)/ Math.pow(2,zoom)
            return 40075016.685578 / Math.Pow(2, zoom);
        }

        private Vector3 LatLonToMeters(double lat, double lon)
        {
            var posx = lon * _originShift / 180.0;
            var posy = Math.Log(Math.Tan((90.0 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0);
            posy = posy * _originShift / 180.0;
            return new Vector3(posx, posy);
        }

        private int Long2Tile(double lon, int zoom)
        {
            return (int)Math.Floor((lon + 180) / 360 * Math.Pow(2, zoom));
        }

        private int Lat2Tile(double lat, int zoom)
        {
            return (int)Math.Floor((1 - Math.Log(Math.Tan(lat * Math.PI / 180) + 1 / Math.Cos(lat * Math.PI / 180)) / Math.PI) / 2 * Math.Pow(2, zoom));
        }

        private Vector3 TileIdToCenterWebMercator(int x, int y, int zoom)
        {
            var tileCnt = Math.Pow(2, zoom);
            var centerX = x + 0.5;
            var centerY = y + 0.5;

            centerX = ((centerX / tileCnt * 2) - 1) * _webMercMax;
            centerY = (1 - (centerY / tileCnt * 2)) * _webMercMax;
            return new Vector3(centerX, centerY);
        }

        public Elements.Geometry.Polygon Boundary { get; }
        public Elements.Topography Topography { get; }
    }
}
