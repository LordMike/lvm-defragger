using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnumsNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApp11
{
    class Program
    {
        static void Main(string[] args)
        {
            JObject obj = JsonConvert.DeserializeObject<JObject>(File.ReadAllText("pvs.json"));
            List<Segment> segments = Parse(obj);

            foreach (IGrouping<string, Segment> lv in segments.GroupBy(s => s.LV).OrderByDescending(s => s.Sum(x => x.Size)))
            {
                Console.WriteLine(lv.Key + " " + lv.Count() + " " + lv.Sum(s => s.Size));
            }

            Console.WriteLine();

            while (true)
            {
                // Find one to move
                var groups = segments
                    .Where(s => s.Type == SegmentType.Linear)
                    .GroupBy(s => s.LV)
                    .Where(s => s.Count() > 1)
                    .Select(s => new { Name = s.Key, Size = s.Sum(x => x.Size), Fragments = s.Count() })
                    .ToList();

                if (!groups.Any())
                    break;

                Segment smallestFree = null;
                string lvToDefrag = null;
                foreach (var fragmented in groups.OrderBy(s => s.Size))
                {
                    smallestFree = segments
                        .Where(s => s.Type == SegmentType.Free && s.Size >= fragmented.Size)
                        .OrderBy(s => s.Size)
                        .FirstOrDefault();

                    if (smallestFree != null)
                    {
                        lvToDefrag = fragmented.Name;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(lvToDefrag) && smallestFree != null)
                {
                    List<Segment> fragments = segments.Where(s => s.LV == lvToDefrag).OrderBy(s => s.LVStart).ToList();
                    uint lvSize = (uint)fragments.Sum(s => s.Size);

                    // Command to move
                    string command = "pvmove --alloc anywhere ";
                    command += $"{smallestFree.Device}:{string.Join(":", fragments.Select(s => s.DeviceOffset + "+" + s.Size))} ";
                    command += $"{smallestFree.Device}:{smallestFree.DeviceOffset}+{fragments.Sum(s => s.Size)}";

                    Console.WriteLine($"# {lvToDefrag} size: {lvSize}");
                    Console.WriteLine(command);

                    // Replace all fragments with free
                    foreach (Segment fragment in fragments)
                    {
                        fragment.Type = SegmentType.Free;
                        fragment.LV = "";
                        fragment.LVStart = 0;
                    }

                    // Split new free into two parts
                    Segment newLv = new Segment
                    {
                        LV = lvToDefrag,
                        Size = lvSize,
                        Device = smallestFree.Device,
                        DeviceOffset = smallestFree.DeviceOffset,
                        Type = SegmentType.Linear,
                        LVStart = 0
                    };

                    segments.Add(newLv);

                    smallestFree.DeviceOffset += lvSize;
                    smallestFree.Size -= lvSize;

                    // Pack
                    segments.Sort((a, b) =>
                    {
                        int compareTo = a.Device.CompareTo(b.Device);
                        if (compareTo == 0)
                            compareTo = a.DeviceOffset.CompareTo(b.DeviceOffset);

                        return compareTo;
                    });

                    Pack(segments);
                }
                else
                {
                    break;
                }
            }

            Console.WriteLine();
        }

        static void Pack(List<Segment> segments)
        {
            for (int i = 1; i < segments.Count; i++)
            {
                // Merge if type, lv end+start, device end+start, name, device match
                Segment previous = segments[i - 1];
                Segment next = segments[i];

                if (previous.Type == next.Type &&
                    previous.Device == next.Device &&
                    previous.LV == next.LV &&
                    previous.LVStart + previous.Size == next.LVStart &&
                    previous.DeviceOffset + previous.Size == next.DeviceOffset)
                {
                    previous.Size += next.Size;
                    segments.RemoveAt(i);
                    i--;
                }
            }
        }

        static List<Segment> Parse(JObject obj)
        {
            JArray list = ((obj["report"] as JArray)[0] as JObject)["pv"] as JArray;

            List<Segment> res = new List<Segment>();

            foreach (JToken token in list)
            {
                Segment seg = new Segment
                {
                    Type = Enums.Parse<SegmentType>(token.Value<string>("segtype"), true),
                    LV = token.Value<string>("lv_name"),
                    LVStart = token.Value<uint>("seg_start_pe"),
                    Size = token.Value<uint>("pvseg_size"),
                    Device = token.Value<string>("pv_name"),
                    DeviceOffset = token.Value<uint>("pvseg_start")
                };

                res.Add(seg);
            }

            return res;
        }
    }
}
