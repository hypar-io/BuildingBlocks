using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using GeometryEx;
using RoomKit;

namespace Story
{
  	public static class Story
	{
		/// <summary>
		/// The Story function.
		/// </summary>
		/// <param name="model">The model. 
		/// Add elements to the model to have them persisted.</param>
		/// <param name="input">The arguments to the execution.</param>
		/// <returns>A StoryOutputs instance containing computed results.</returns>
		public static StoryOutputs Execute(Dictionary<string, Model> models, StoryInputs input)
		{
            var masses = models.ContainsKey("envelope") ? models["envelope"].AllElementsOfType<Mass>().ToList().FindAll(m => m.Name == "envelope") : new List<Mass>();
            var outputModel = new Model();
            if (masses.Count() == 0)
            {
                var mass = new Mass(Polygon.Rectangle(50.0, 50.0),
                                    10.0,
                                    new Material("basement", Palette.Gray, 0.0f, 0.0f), name: "envelope");
                masses.Add(mass);
                outputModel.AddElement(mass);
                mass = new Mass(Polygon.Rectangle(50.0, 50.0),
                                75.0,
                                new Material("envelope", Palette.Aqua, 0.0f, 0.0f), name: "envelope");
                masses.Add(mass);
                outputModel.AddElement(mass);
            }
            var storyHeight = input.StoryHeight;
            var stories = new List<RoomKit.Story>();
            var heightLimit = 0.0;
            foreach (var mass in masses)
            {
                heightLimit += mass.Height;
                var storyQty = mass.Height / input.StoryHeight;
                if (storyQty <= 1.0)
                {
                    storyQty = 1.0;
                    storyHeight = mass.Height;
                }
                else
                {
                    storyQty = Math.Floor(storyQty);
                }
                var perimeters = mass.ProfileTransformed().Perimeter.Offset(input.EnvelopeOffset * -1);
                if (perimeters.Count() == 0)
                {
                    continue;
                }
                var perimeter = perimeters.First();
                var elevation = mass.Transform.Origin.Z;
                var stack = new Tower((int)storyQty,
                                      heightLimit,
                                      "",
                                      perimeter,
                                      storyHeight,
                                      perimeter.Area() * storyQty)
                {
                    Elevation = elevation
                };
                stack.Stack();
                stories.AddRange(stack.Stories);
            }
            var core = masses.Find(m => m.Name == "core");
            foreach (var story in stories)
            {
                if (core != null)
                {
                    story.AddExclusion(new Room(perimeter: core.ProfileTransformed().Perimeter)
                    {
                        Name = "core",
                        Height = story.Height
                    });
                }
                story.Name = "story";
                var line = story.Perimeter.Segments().ToList().OrderBy(s => s.Length()).Skip(3).First();
                var angle = Math.Atan2(line.End.Y - line.Start.Y, line.End.X - line.Start.X) * (180 / Math.PI);
                story.Rotate(story.Perimeter.Centroid(), angle * -1);
                story.RoomsByDivision((int)input.RoomsPerStory / 2, 2, story.Height - 1.0, 0.5);
                var box = new TopoBox(story.Perimeter);
                var corridor = new Line(new Vector3(box.W.X + 0.5, box.W.Y),
                                        new Vector3(box.E.X - 0.5, box.E.Y)).Thicken(input.CorridorWidth);
                story.AddCorridor(new Room(perimeter: corridor, height: story.Height - 1.0));
                story.Rotate(story.Perimeter.Centroid(), angle);
                outputModel.AddElement(story.EnvelopeAsMass);
                outputModel.AddElement(story.Slab);
                foreach (var room in story.Rooms)
                {
                    outputModel.AddElement(room.AsSpace);
                }
                foreach (var room in story.Corridors)
                {
                    outputModel.AddElement(room.AsSpace);
                }
            }
            return new StoryOutputs(stories.Count, stories.Count * input.RoomsPerStory)
            {
                model = outputModel
            };
		}

    }
}