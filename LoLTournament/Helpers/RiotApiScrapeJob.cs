﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Configuration;
using LoLTournament.Models;
using RiotSharp;
using RiotSharp.LeagueEndpoint;
using RiotSharp.StatsEndpoint;
using RiotSharp.SummonerEndpoint;
using Season = RiotSharp.StatsEndpoint.Season;
using System.Globalization;
using System.Threading;

namespace LoLTournament.Helpers
{
    public class RiotApiScrapeJob
    {
        private readonly RiotApi _api;
        private readonly StaticRiotApi _staticApi;

        public RiotApiScrapeJob()
        {
            var key = WebConfigurationManager.AppSettings["RiotApiKey"];
            var rateLimit1 = int.Parse(WebConfigurationManager.AppSettings["RateLimitPer10Seconds"]);
            var rateLimit2 = int.Parse(WebConfigurationManager.AppSettings["RateLimitPer10Minutes"]);

            _api = RiotApi.GetInstance(key, rateLimit1, rateLimit2);
            _staticApi = StaticRiotApi.GetInstance(key);
        }

        public void StartTimer()
        {
            new Timer(ScrapeSummoners, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
            new Timer(ScrapeStatic, null, TimeSpan.Zero, TimeSpan.FromDays(1.0));
        }

        public void ScrapeStatic(object arg)
        {
            Mongo.Champions.DropAllIndexes();
            var champions = _staticApi.GetChampions(Region.euw).Champions;
            foreach(var champion in champions.Keys)
            {
                Mongo.Champions.Save(new Champion { Name = champions[champion].Name, ChampionId = champions[champion].Id });
            }
        }

        /// <summary>
        /// Scrapes the summoner statistics for all participants from the Riot API.
        /// </summary>
        /// <param name="arg">Unused</param>
        public void ScrapeSummoners(object arg)
        {
            // Disable during tournament to save API calls
            var timeSetting = WebConfigurationManager.AppSettings["TournamentStart"];
            var tournamentStart = DateTime.ParseExact(timeSetting, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            if (DateTime.Now >= tournamentStart && DateTime.Now <= tournamentStart + TimeSpan.FromHours(12))
                // assuming tournament lasts for a maximum of 12 hours
                return;

            var participants = Mongo.Participants.FindAll();

            // Loop through participants and retrieve their summoner and league information
            foreach (var p in participants)
            {
                // Get summoner information
                Summoner s;
                try
                {
                    s = _api.GetSummoner(Region.euw, p.SummonerName);
                }
                catch (Exception)
                {
                    // Summoner does not exist or Riot API offline
                    continue;
                }

                // Get previous season league
                var previousTier = Tier.Unranked;
                try
                {
                    var rankedGames = _api.GetMatchList(Region.euw, s.Id, null, new List<Queue> {Queue.RankedSolo5x5});

                    if (rankedGames != null && rankedGames.Matches.Count > 0)
                    {
                        var id = rankedGames.Matches.First().MatchID;
                        var match = _api.GetMatch(Region.euw, id);
                        if (match != null)
                            previousTier = match.Participants.Single(x => x.ParticipantId == match.ParticipantIdentities.Single(y => y.Player.SummonerId == s.Id).ParticipantId).HighestAchievedSeasonTier;
                    }
                }
                catch (Exception ex)
                {
                    // Unranked in previous season
                }

                // Get wins and losses in previous season
                int wins;
                int losses;
                try
                {
                    var winLoss = _api.GetStatsSummaries(Region.euw, s.Id, Season.Season2015); // change each year to PREVIOUS season
                    var winLossSoloQueue =
                        winLoss.Single(x => x.PlayerStatSummaryType == PlayerStatsSummaryType.RankedSolo5x5);
                    wins = winLossSoloQueue.Wins;
                    losses = winLossSoloQueue.Losses;
                }
                catch (Exception ex)
                {
                    // No matches played in previous season
                    wins = 0;
                    losses = 0;
                }

                // Get wins and losses in current season
                int winsCurrent;
                int lossesCurrent;
                try
                {
                    var winLoss = _api.GetStatsSummaries(Region.euw, s.Id, Season.Season2016); // change each year to CURRENT season
                    var winLossSoloQueue =
                        winLoss.Single(x => x.PlayerStatSummaryType == PlayerStatsSummaryType.RankedSolo5x5);
                    winsCurrent = winLossSoloQueue.Wins;
                    lossesCurrent = winLossSoloQueue.Losses;
                }
                catch (Exception ex)
                {
                    // No matches played in previous season
                    winsCurrent = 0;
                    lossesCurrent = 0;
                }

                // Get current league
                Tier tier;
                int divisionInt;
                try
                {
                    var currentLeague = _api.GetLeagues(Region.euw, new List<int> {(int) s.Id});
                    var currentLeagueSoloQueue = currentLeague.First().Value.Single(x => x.Queue == Queue.RankedSolo5x5);
                    tier = currentLeagueSoloQueue.Tier;
                    var division =
                        currentLeagueSoloQueue.Entries.Single(x => x.PlayerOrTeamId == s.Id.ToString()).Division;

                    divisionInt = RomanToInt(division);
                }
                catch (Exception)
                {
                    // Unranked in current season
                    tier = Tier.Unranked;
                    divisionInt = 0;
                }

                // Set values and update DB
                p.Summoner = s;
                p.PreviousSeasonWins = wins;
                p.PreviousSeasonLosses = losses;
                p.PreviousSeasonTier = previousTier;
                p.CurrentSeasonTier = tier;
                p.CurrentSeasonDivision = divisionInt;
                p.CurrentSeasonWins = winsCurrent;
                p.CurrentSeasonLosses = lossesCurrent;
                p.LastUpdateTime = DateTime.UtcNow;


                Mongo.Participants.Save(p);
            }
            
            // Check official registration status
            RegistrantsCsvParser.Check();
        }

        /// <summary>
        /// Converts a Roman numeral between 1 and 5 to an integer.
        /// </summary>
        /// <param name="numeral">A roman numeral between 1 and 5, inclusive.</param>
        /// <returns>An integer between 1 and 5. 0 if invalid argument.</returns>
        private static int RomanToInt(string numeral)
        {
            switch (numeral)
            {
                case "I":
                    return 1;
                case "II":
                    return 2;
                case "III":
                    return 3;
                case "IV":
                    return 4;
                case "V":
                    return 5;
                default:
                    return 0;
            }
        }
    }
}