using System;
namespace WhiteCow.Extension
{
    public static class Extension
    {
		public static Int64 getUnixTime(this DateTime dt)
		{
			return (Int64)(dt - new DateTime(1970, 1, 1)).TotalSeconds;
		}
    }
}
