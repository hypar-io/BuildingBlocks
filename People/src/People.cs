using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace People
{
    public static class People
    {
        static readonly List<ContentElement> availableFriends = (List<ContentElement>)Model.FromJson(File.ReadAllText("./24fce03a-3497-4af3-be80-ed04b85b1a01.json")).AllElementsOfType<ContentCatalog>().First().Content;
        static readonly Random random = new Random();

        public static PeopleOutputs Execute(Dictionary<string, Model> inputModels, PeopleInputs input)
        {
            var output = new PeopleOutputs();

            if (!inputModels.TryGetValue("Floors", out var floorsModel))
            {
                output.Errors.Add("The model output named 'Floors' could not be found. Check the upstream functions for errors.");
                return output;
            }

            // ###### GET ALL OF THE FLOORS FROM THE MODEL
            var floors = floorsModel.AllElementsOfType<Floor>();

            if (floors.Count() == 0)
            {
                output.Errors.Add($"No Floors found in the model 'Floors'. Check the output from the function upstream that has a model output 'Floors'.");
                return output;
            }

            // ###### ITERATE OVER ALL OF THE FLOORS
            // ###### ###### GENERATE THE X AND Y DOMAINS
            // ###### ###### GENERATE POINTS, CHECK IF THEY ARE VALID
            // ###### ###### IF VALID, ADD AN ENTOURAGE ELEMENT INSTANCE
            foreach (var floor in floors)
            {
                GetXandYDomains(floor, out Domain1d xDomain, out Domain1d yDomain);
                var offsetPerimeter = floor.Profile.Perimeter.Offset(-1)[0];

                var friendsGenerated = new List<ElementInstance>();

                while (friendsGenerated.Count < input.PeoplePerFloor)
                {
                    var x = random.NextDouble().MapToDomain(xDomain);
                    var y = random.NextDouble().MapToDomain(yDomain);
                    var point = new Vector3(x, y, 0);
                    if (offsetPerimeter.Contains(point))
                    {
                        var friend = GetNextFriend(input.IncludeSeatedPeople);
                        var t = new Transform();
                        t.Rotate(Vector3.ZAxis, random.NextDouble() * 180.0);
                        t.Move(new Vector3(point.X, point.Y, floor.Elevation + floor.Thickness));
                        var friendInstance = friend.CreateInstance(t, floor.Name);
                        friendsGenerated.Add(friendInstance);
                    }
                }

                output.Model.AddElements(friendsGenerated);
            }

            return output;
        }

        private static void GetXandYDomains(Floor floor, out Domain1d xDomain, out Domain1d yDomain)
        {
            var xMax = floor.Profile.Perimeter.Vertices.Select(v => v.X).Max();
            var xMin = floor.Profile.Perimeter.Vertices.Select(v => v.X).Min();
            var yMax = floor.Profile.Perimeter.Vertices.Select(v => v.Y).Max();
            var yMin = floor.Profile.Perimeter.Vertices.Select(v => v.Y).Min();

            xDomain = new Domain1d(xMin, xMax);
            yDomain = new Domain1d(yMin, yMax);
        }

        public static ContentElement GetNextFriend(bool includeSeated)
        {
            var index = random.Next(availableFriends.Count - 1);
            var friend = availableFriends[index];
            return includeSeated && friend.Name.Contains("sitting") ? GetNextFriend(includeSeated) : friend;
        }
    }
}