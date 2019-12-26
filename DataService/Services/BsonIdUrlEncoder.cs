﻿using System;
using LiteDB;

namespace maxbl4.Race.DataService.Services
{
    public static class BsonIdUrlEncoder
    {
        public static BsonValue Decode(string urlEncodedId)
        {
            if (urlEncodedId.EndsWith("L", StringComparison.OrdinalIgnoreCase)
                && long.TryParse(Strip(urlEncodedId), out var l))
                return new BsonValue(l);
            if (urlEncodedId.EndsWith("G", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(Strip(urlEncodedId), out var g))
                return new BsonValue(g);
            if (urlEncodedId.EndsWith("o", StringComparison.OrdinalIgnoreCase))
                return new ObjectId(Strip(urlEncodedId));
            if (int.TryParse(urlEncodedId, out var i))
                return i;
            
            throw new ArgumentException("Failed to recognize id. Valid examples: int => 1, long => 88888888999L, guid => 079FC22F-ED55-4D7A-8DB7-70EB5FCF0D1Ag, objectId => 1234567890abcdef12345678o");
        }

        public static string Encode(BsonValue id)
        {
            switch (id.Type)
            {
                case BsonType.Int32:
                    return id.AsInt32.ToString();
                case BsonType.Int64:
                    return id.AsInt64 + "L";
                case BsonType.ObjectId:
                    return id.AsObjectId + "o";
                case BsonType.Guid:
                    return id.AsGuid.ToString("N") + "g";
                default:
                    throw new ArgumentOutOfRangeException("id.Type", id.Type.ToString());
            }
        }

        private static string Strip(string urlEncodedId)
        {
            return urlEncodedId.Substring(0, urlEncodedId.Length - 1);
        }
    }
}