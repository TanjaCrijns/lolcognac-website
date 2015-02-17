﻿using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace LoLTournament.Models
{
    public class Match
    {
        public ObjectId Id { get; set; }
        public long RiotMatchId { get; set; }

        public ObjectId BlueTeamId { get; set; }
        public ObjectId PurpleTeamId { get; set; }
        public Phase Phase { get; set; }
        public int Priority { get; set; } // used to indicate order for Pool and Finale (best out of three) phase

        public bool Finished { get; set; }

        public ObjectId WinnerId { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime FinishTime { get; set; }

        public int KillsBlueTeam { get; set; }
        public int DeathsBlueTeam { get; set; }
        public int AssistsBlueTeam { get; set; }

        public int KillsPurpleTeam { get; set; }
        public int DeathsPurpleTeam { get; set; }
        public int AssistsPurpleTeam { get; set; }

        [BsonIgnore]
        public Team BlueTeam {
            get
            {
                var client = new MongoClient();
                var server = client.GetServer();
                var db = server.GetDatabase("CLT");
                var col = db.GetCollection<Team>("Teams");

                return col.Find(Query<Team>.Where(x => x.Id == BlueTeamId)).FirstOrDefault();
            }
        }

        [BsonIgnore]
        public Team PurpleTeam
        {
            get
            {
                var client = new MongoClient();
                var server = client.GetServer();
                var db = server.GetDatabase("CLT");
                var col = db.GetCollection<Team>("Teams");

                return col.Find(Query<Team>.Where(x => x.Id == PurpleTeamId)).FirstOrDefault();
            }
        }

        [BsonIgnore]
        public Team Winner
        {
            get
            {
                var client = new MongoClient();
                var server = client.GetServer();
                var db = server.GetDatabase("CLT");
                var col = db.GetCollection<Team>("Teams");

                return col.Find(Query<Team>.Where(x => x.Id == WinnerId)).FirstOrDefault();
            }
        }
    }
}