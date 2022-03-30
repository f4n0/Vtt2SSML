using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vtt2SSML
{
  public class Vtt
  {
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Speaker { get; set; }
    public string Text { get; set; }
  }
}
