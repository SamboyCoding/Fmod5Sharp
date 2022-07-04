using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fmod5Sharp.Util;

[JsonSerializable(typeof(Dictionary<uint, FmodVorbisData>))]
internal partial class Fmod5SharpJsonContext : JsonSerializerContext
{
    
}