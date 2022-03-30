// See https://aka.ms/new-console-template for more information
using vtt2SSML;
using CognitiveServices.Translator;
using CognitiveServices.Translator.Configuration;
using CognitiveServices.Translator.Translate;
using System.Globalization;
using System.Text.RegularExpressions;

var internalCounter = 0;
var Vtts = new List<Vtt>();
bool translate = false;
var fromLang = "it-it";
var toLang = "en-us";
bool WebvttHdr = false;
var separator = "";

Console.Write("The vtt file path: ");
var OriginalFileName = Console.ReadLine().Trim('"');
var lines = File.ReadAllLines(OriginalFileName);

Console.Write("Do you want to translate the text? (azure translation) (y or n): ");
translate = (Console.ReadLine()?.ToLower() == "y" ? true : false);
if (translate)
{
  Console.Write("Source language (eg. it-it): ");
  fromLang = Console.ReadLine();

  Console.Write("Destination language (eg. en-en): ");
  toLang = Console.ReadLine();
}

Console.Write("What's the separator for timestamps?");
Console.WriteLine("What's the separator for timestamps?");
Console.WriteLine("Option 1: \" --> \"");
Console.WriteLine("Option 2: \" -> \"");
Console.WriteLine("Option 3: custom");
Console.Write("Choose the format: (1, 2 or 3): ");
switch (Console.ReadLine())
{
  case "1":
    separator = " --> ";
    break;
  case "2":
    separator = " -> ";
    break;
  case "3":
    separator = Console.ReadLine()?.Trim('"');
    break;
}

Console.Write("Does the file starts with WEBVTT? (y or n): ");
WebvttHdr = (Console.ReadLine()?.ToLower() == "y" ? true : false);

Vtt tmp = new Vtt();

Console.WriteLine("Whats the format of the file? (1 or 2)");
Console.WriteLine("Option 1: \n timestamp \n Speaker \n Text");
Console.WriteLine("Option 2: \n timestamp \n Text");
Console.WriteLine("Option 3: \n timestamp \n <v Speaker>Text</v> \n White line");
Console.Write("Choose the format: (1, 2 or 3): ");
switch (Console.ReadLine())
{
  case "1":
    for (var i = 0; i < lines.Length; i += 1)
    {
      if (WebvttHdr && string.Equals(lines[i], "WEBVTT", StringComparison.OrdinalIgnoreCase))
      {
        Console.WriteLine("Skipping header");
      }
      else
      {
        var line = lines[i];
        switch (internalCounter)
        {
          case 0: //time 00: 00: 00.000 -> 00: 00: 03.390 
            if (line != "")
            {
              var test = line.Replace(": ", ":");
              var split = test.Split(separator);
              tmp.Start = TimeSpan.Parse(split[0].Trim());
              if (split[1].Length < 12)
              {
                // file broken --> check  next line if helps;
                var testNextLine = lines[i + 1];
                split[1] = split[1].Trim() + testNextLine.Substring(0, (12 - split[1].Length));
                lines[i + 1] = testNextLine.Substring(12 - split[1].Length);
              }
              tmp.End = TimeSpan.Parse(split[1].Trim());
            }
            internalCounter++;
            break;
          case 1: //Speaker
            tmp.Speaker = line;
            internalCounter++;
            break;
          case 2: //Text
            tmp.Text = line;
            internalCounter = 0;
            Vtts.Add(tmp);
            tmp = new Vtt();
            break;
        }
      }
    }
    break;

  case "2":
    for (var i = 0; i < lines.Length; i += 1)
    {
      if (WebvttHdr && string.Equals(lines[i], "WEBVTT", StringComparison.OrdinalIgnoreCase))
      {
        Console.WriteLine("Skipping header");
      }
      else
      {
        var line = lines[i];
        switch (internalCounter)
        {
          case 0: //time 00: 00: 00.000 -> 00: 00: 03.390 
            if (line != "")
            {
              var test = line.Replace(": ", ":");
              var split = test.Split(separator);
              tmp.Start = TimeSpan.Parse(split[0].Trim());
              if (split[1].Length < 12)
              {
                // file broken --> check  next line if helps;
                var testNextLine = lines[i + 1];
                split[1] = split[1].Trim() + testNextLine.Substring(0, (12 - split[1].Length));
                lines[i + 1] = testNextLine.Substring(12 - split[1].Length);
              }
              tmp.End = TimeSpan.Parse(split[1].Trim());
            }
            internalCounter++;
            break;
          case 1: //Text         
            tmp.Text = line;
            internalCounter = 0;
            Vtts.Add(tmp);
            tmp = new Vtt();
            break;
        }
      }
    }
    break;

  case "3":
    for (var i = 0; i < lines.Length; i += 1)
    {
      if (WebvttHdr && string.Equals(lines[i], "WEBVTT", StringComparison.OrdinalIgnoreCase))
      {
        Console.WriteLine("Skipping header");
      }
      else
      {
        var line = lines[i];
        switch (internalCounter)
        {
          case 0: //time 00: 00: 00.000 -> 00: 00: 03.390 
            if (line != "")
            {
              var test = line.Replace(": ", ":");
              var split = test.Split(separator);
              tmp.Start = TimeSpan.Parse(split[0].Trim());
              if (split[1].Length < 12)
              {
                // file broken --> check  next line if helps;
                var testNextLine = lines[i + 1];
                split[1] = split[1].Trim() + testNextLine.Substring(0, (12 - split[1].Length));
                lines[i + 1] = testNextLine.Substring(12 - split[1].Length);
              }
              tmp.End = TimeSpan.Parse(split[1].Trim());
            }
            internalCounter++;
            break;
          case 1: //Text
            var regex = new Regex("(<v .*?>)(.*?)(<\\/v>)", RegexOptions.IgnoreCase);
            tmp.Text = regex.Match(line).Groups[2].Value;
            internalCounter++;
            Vtts.Add(tmp);
            tmp = new Vtt();
            break;
          case 2: //white line
            internalCounter = 0;
            //do nothing
            break;
        }
      }
    }
    break;

}


using (StreamWriter sw = File.CreateText(OriginalFileName + "_converted.ssml"))
{
  var chunks = Split(Vtts, 25);

  TimeSpan LastEnd = TimeSpan.Zero;
  foreach (var chunk in chunks)
  {
    var chunkArr = chunk.ToArray();
    var sourceTranslation = chunk.Select(o => new RequestContent(o.Text));
    if (translate)
    {
      var translated = (await Translate(sourceTranslation, fromLang, toLang)).ToList();

      foreach (var item in translated)
      {
        var vtt = chunkArr[translated.IndexOf(item)];

        if (LastEnd != TimeSpan.Zero)
        {
          var test = vtt.Start.Subtract(LastEnd);
          test.TotalMilliseconds.ToString();
          sw.WriteLine($"<break time=\"{test.TotalMilliseconds}ms\" />");
          LastEnd = vtt.End;
        }
        else
        {
          LastEnd = vtt.End;
        }

        sw.WriteLine(item.Translations?.FirstOrDefault()?.Text);
      }
    }
    else
    {
      foreach (var item in chunkArr)
      {
        if (LastEnd != TimeSpan.Zero)
        {
          var test = item.Start.Subtract(LastEnd);
          test.TotalMilliseconds.ToString();
          sw.WriteLine($"<break time=\"{test.TotalMilliseconds}ms\" />");
          LastEnd = item.End;
        }
        else
        {
          LastEnd = item.End;
        }
        sw.WriteLine(item.Text);
      }
    }
  }

}

Console.WriteLine("Converted!");
Console.WriteLine("press any key to exit");
Console.ReadKey();

static IEnumerable<IEnumerable<T>> Split<T>(IEnumerable<T> source, int chunkSize)
{
  return source
      .Select((x, i) => new { Value = x, Index = i })
      .GroupBy(x => x.Index / chunkSize)
      .Select(g => g.Select(x => x.Value));
}

async Task<IEnumerable<ResponseBody>> Translate(IEnumerable<RequestContent> original, string fromLang, string toLang)
{
  var client = new TranslateClient(new CognitiveServicesConfig
  {
    Name = "Translator",
    SubscriptionKey = "insert API KEY"
  });

  var response = await client.TranslateAsync(original, new RequestParameter
  {
    From = fromLang,
    To = new[] { toLang },
    IncludeAlignment = true
  });
  return response;
}