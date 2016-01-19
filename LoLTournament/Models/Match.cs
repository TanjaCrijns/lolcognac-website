﻿using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using LoLTournament.Helpers;

namespace LoLTournament.Models
{
    public class Match
    {
        public ObjectId Id { get; set; }
        public long RiotMatchId { get; set; }

        public ObjectId BlueTeamId { get; set; }
        public ObjectId RedTeamId { get; set; }
        public Phase Phase { get; set; }
        public int Priority { get; set; } // used to indicate order for Pool and Finale (best out of three) phase

        public bool Finished { get; set; }

        public ObjectId WinnerId { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The date that the data was entered into the system.
        /// </summary>
        public DateTime FinishDate { get; set; }

        public long KillsBlueTeam { get; set; }
        public long DeathsBlueTeam { get; set; }
        public long AssistsBlueTeam { get; set; }
        public int[] ChampionIds { get; set; }
        public int[] BanIds { get; set; }

        public long KillsRedTeam { get; set; }
        public long DeathsRedTeam { get; set; }
        public long AssistsRedTeam { get; set; }

        public string TournamentCode { get; set; }
        public string TournamentCodeBlind { get; set; }
        public bool Invalid { get; set; }
        public string InvalidReason { get; set; }

        public bool PlayedWrongSide { get; set; }

        [BsonIgnore]
        public Team BlueTeam {
            get
            {
                return Mongo.Teams.Find(Query<Team>.Where(x => x.Id == BlueTeamId)).FirstOrDefault();
            }
        }

        [BsonIgnore]
        public Team RedTeam
        {
            get
            {
                return Mongo.Teams.Find(Query<Team>.Where(x => x.Id == RedTeamId)).FirstOrDefault();
            }
        }

        [BsonIgnore]
        public Team Winner
        {
            get
            {
                return Mongo.Teams.Find(Query<Team>.Where(x => x.Id == WinnerId)).FirstOrDefault();
            }
        }
    }
}
