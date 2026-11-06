using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;

// Compile and execute the production engine, not a reimplementation of its rules.
var root = Path.GetFullPath(args.Length > 0 ? args[0] : "../..");
var output = Path.GetFullPath(args.Length > 1 ? args[1] : Path.Combine(root, "Artifacts/BalanceLab/WZ1-alpha-1"));
Directory.CreateDirectory(output);
var loaded = CardCatalogLoader.LoadFromDirectory(Path.Combine(root, "Assets/StreamingAssets/Data"));
if (!loaded.Success) throw new Exception("Production catalog/manifest validation failed.");
var catalog = loaded.Catalog;
var expanded = args.Contains("--expanded");
var advanced = args.Contains("--advanced") || expanded;
var effects = args.Contains("--effects") || advanced;
var preview = args.Contains("--preview") || effects;
var rules = expanded ? RuleConfig.ExpandedDefaults() : advanced ? RuleConfig.AdvancedDefaults() : effects ? RuleConfig.EffectsDefaults() : preview ? RuleConfig.PreviewDefaults() : RuleConfig.AlphaDefaults();
var decks = StarterDeckCatalog.Load(Path.Combine(root, "Assets/StreamingAssets/Data", StarterDeckCatalog.FileName), catalog, rules).ToArray();
foreach (var deck in decks)
{
    var validation = DeckValidator.Validate(deck, catalog, rules);
    if (!validation.IsValid) throw new Exception(deck.DeckId + ": " + validation.Reason);
    foreach (var id in deck.MainDeckCardIds)
        if (!catalog.TryGet(id, out var card) || card.Type != CardType.Character) throw new Exception("Non-vanilla starter card " + id);
}

var rows = new List<MatchRow>();
if (expanded)
    decks = decks.Select(d => new DeckList(d.DeckId + "-V05", d.DisplayName + " v0.5", d.HeroCardId, d.ClassCardId,
        d.MainDeckCardIds.Skip(17).Concat(catalog.All.Where(c => c.Type != CardType.Character && (EffectEngine.Supports(c) || AdvancedEffects.Supports(c) || ExpandedEffects.Supports(c))).Select(c => c.CardId)).ToArray())).ToArray();
else if (advanced)
    decks = decks.Select(d => new DeckList(d.DeckId + "-ADV", d.DisplayName + " Advanced", d.HeroCardId, d.ClassCardId,
        d.MainDeckCardIds.Skip(14).Concat(catalog.All.Where(c => c.Type != CardType.Character && (EffectEngine.Supports(c) || AdvancedEffects.Supports(c))).Select(c => c.CardId)).ToArray())).ToArray();
else if (effects)
    decks = decks.Select(d => new DeckList(d.DeckId + "-FX", d.DisplayName + " FX", d.HeroCardId, d.ClassCardId,
        d.MainDeckCardIds.Skip(8).Concat(new[] {"WZ1-078", "WZ1-079", "WZ1-082", "WZ1-086"}.SelectMany(id => Enumerable.Repeat(new CardId(id), 2))).ToArray())).ToArray();
var traces = new List<object>();
var pairs = new[] { (0, 1), (0, 2), (1, 2) };
for (var pairIndex = 0; pairIndex < 50; pairIndex++)
{
    var (a, b) = pairs[pairIndex % 3];
    var seed = 2026110500UL + (ulong)pairIndex;
    for (var swap = 0; swap < 2; swap++)
    {
        var first = decks[swap == 0 ? a : b];
        var second = decks[swap == 0 ? b : a];
        var run = Simulate(seed, first, second, catalog, rules);
        var replay = Simulate(seed, first, second, catalog, rules);
        if (JsonSerializer.Serialize(run.Row) != JsonSerializer.Serialize(replay.Row) || !run.Trace.SequenceEqual(replay.Trace)) throw new Exception("Determinism failure " + seed);
        rows.Add(run.Row);
        traces.Add(new { seed, deck0 = first.DeckId, deck1 = second.DeckId, actions = run.Trace });
    }
}
var options = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(Path.Combine(output, "matches.json"), JsonSerializer.Serialize(rows, options));
File.WriteAllText(Path.Combine(output, "traces.json"), JsonSerializer.Serialize(traces, options));
File.WriteAllLines(Path.Combine(output, "matches.csv"), new[] {"seed,deck0,deck1,firstPlayer,winner,turns,commands,plays,attacks,heroAttacks,reorganizations,rearPlays,interceptions,cause,hash"}.Concat(rows.Select(r =>
    $"{r.Seed},{r.Deck0},{r.Deck1},{r.FirstPlayer},{r.Winner},{r.Turns},{r.Commands},{r.Plays},{r.Attacks},{r.HeroAttacks},{r.Reorganizations},{r.RearPlays},{r.Interceptions},{r.Cause},{r.Hash}")));
var summary = new {
    sourceCommit = expanded ? "8803f11 + expanded-effects changes" : "b84f73a + advanced-effects changes",
    ruleVersion = rules.RuleVersion,
    scope = "Production engine; --advanced adds 14 and --expanded adds 17 supported non-Characters once per deck. Heuristic bots; not full-set balance validation.",
    completed = rows.Count, repeatedIdentically = rows.Count, rejectedCommands = 0, invariantFailures = 0,
    catalogSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,"Assets/StreamingAssets/Data/cards_wz1.v0.1.json")))).ToLowerInvariant(),
    minimumTurns = rows.Min(r => r.Turns), meanTurns = rows.Average(r => r.Turns), maximumTurns = rows.Max(r => r.Turns),
    firstPlayerWins = rows.Count(r => r.FirstPlayer == r.Winner),
    commandCount = rows.Sum(r => r.Commands), rearPlays = rows.Sum(r => r.RearPlays), reorganizations = rows.Sum(r => r.Reorganizations),
    interceptions = rows.Sum(r => r.Interceptions), gamesWithFailedDraws = rows.Count(r=>r.FailedDraws>0), failedDraws = rows.Sum(r=>r.FailedDraws),
    actionsResolved = rows.Sum(r => r.ActionsResolved),
    reactionsPlayed = rows.Sum(r => r.ReactionsPlayed), responsesUsed = rows.Sum(r => r.ResponsesUsed),
    equipmentAttached = rows.Sum(r => r.EquipmentAttached), triggeredEffects = rows.Sum(r => r.TriggeredEffects), choicesMade = rows.Sum(r => r.ChoicesMade),
    endCauses = rows.GroupBy(r => r.Cause).ToDictionary(g => g.Key, g => g.Count()),
    finalBlows = rows.GroupBy(r => r.FinalBlow).ToDictionary(g => g.Key, g => g.Count()),
    deckResults = decks.Select(d => new { id = d.DeckId, games = rows.Count(r => r.Deck0 == d.DeckId || r.Deck1 == d.DeckId), wins = rows.Count(r => (r.Winner == 0 ? r.Deck0 : r.Deck1) == d.DeckId), meanCost = d.MainDeckCardIds.Average(id => {catalog.TryGet(id,out var c); return (double)c.CostPa.Value;}) }),
    matchups = rows.GroupBy(r => string.Join(" / ",new[]{r.Deck0,r.Deck1}.OrderBy(x=>x))).Select(g => new {pair=g.Key, games=g.Count(), wins=g.GroupBy(r=>r.Winner==0?r.Deck0:r.Deck1).ToDictionary(w=>w.Key,w=>w.Count())})
};
File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, options));
Console.WriteLine(JsonSerializer.Serialize(summary, options));

// Additional diagnostics are NOT counted among the 100 paired WZ1 matches.
var capped = new Warpaz.Tests.EditMode.Sprint2StressRunner(new Warpaz.Tests.EditMode.Sprint2StressConfig(matchCount:1,maxTurns:0)).Run();
if(capped.MatchesCompleted!=0 || capped.Stalls!=1)throw new Exception("False completion regression");
var legacy = new Warpaz.Tests.EditMode.Sprint2StressRunner().Run();
if(legacy.MatchesCompleted!=50 || legacy.Stalls!=0)throw new Exception("Legacy soak failed: "+legacy);
var cheap = catalog.All.Where(c=>c.Type==CardType.Character && c.Power<5).Take(14).ToArray();
var cheapDeck = new DeckList("DIAG-LOW-POWER","No hero damage",decks[0].HeroCardId,decks[0].ClassCardId,cheap.SelectMany((c,i)=>Enumerable.Repeat(c.CardId,i<12?3:2)).ToArray());
var fatigue = Simulate(99,cheapDeck,cheapDeck,catalog,rules).Row;
if(fatigue.FailedDraws==0 || fatigue.FinalBlow!="failed-draw")throw new Exception("Low-power diagnostic did not reach fatigue");
var deadDeck = new DeckList("DIAG-NONPLAYABLE","Accepted but not playable",decks[0].HeroCardId,decks[0].ClassCardId,catalog.All.Where(c=>c.Type==CardType.Action).Take(14).SelectMany((c,i)=>Enumerable.Repeat(c.CardId,i<12?3:2)).ToArray());
var deadValidation = DeckValidator.Validate(deadDeck,catalog,rules);
MatchRow dead = null;
if (preview)
{
    if (deadValidation.IsValid) throw new Exception("Preview accepted unsupported cards");
    var invalid = new GameSession(catalog);
    var rejected = invalid.Start(new MatchSetup(100, rules, catalog.ContentVersion, new PlayerSetup(PlayerId.Player0,deadDeck), new PlayerSetup(PlayerId.Player1,deadDeck)));
    if (rejected.Accepted || invalid.State != null) throw new Exception("Invalid match left partial state");
}
else
{
    if(!deadValidation.IsValid)throw new Exception("Legacy validator changed");
    dead = Simulate(100,deadDeck,deadDeck,catalog,rules).Row;
    if(dead.Plays!=0 || dead.FinalBlow!="failed-draw")throw new Exception("Unsupported-card diagnostic changed");
}
var probe = new GameSession(catalog);
Require(probe.Start(new MatchSetup(42,rules,catalog.ContentVersion,new PlayerSetup(PlayerId.Player0,decks[0]),new PlayerSetup(PlayerId.Player1,decks[1]))));
Require(probe.ConfirmMulligan(PlayerId.Player0,true)); Require(probe.ConfirmMulligan(PlayerId.Player1,true));
var hashBefore=probe.CurrentHash;
var wrongActor=probe.State.ActivePlayerId==PlayerId.Player0?PlayerId.Player1:PlayerId.Player0;
if(probe.EndTurn(wrongActor).Accepted || probe.CurrentHash!=hashBefore)throw new Exception("Rejected command mutated state");
File.WriteAllText(Path.Combine(output,"diagnostics.json"),JsonSerializer.Serialize(new {turnCapRegression="PASS", rejectedCommandAtomicity="PASS", legacySoakCompleted=legacy.MatchesCompleted, lowPower=fatigue, unsupportedActions=dead},options));
var tested = decks.SelectMany(d=>d.MainDeckCardIds).ToHashSet();
var audit = catalog.All.Select(c=>new {id=c.CardId.ToString(),c.Name,type=c.Type.ToString(),c.CostPa,c.Power,c.Position,c.Unique,keywords=c.Keywords,effectCount=c.Effects.Count,starterIncluded=tested.Contains(c.CardId),
    textProgram = EffectEngine.Supports(c) ? "v0.3" : AdvancedEffects.Supports(c) ? "v0.4" : ExpandedEffects.Supports(c) ? "v0.5" : "none",
    statDominatedBy=c.Type==CardType.Character?catalog.All.Where(o=>o.Type==CardType.Character && o.CardId!=c.CardId && o.Position==c.Position && o.CostPa<=c.CostPa && o.Power>=c.Power && (o.CostPa<c.CostPa || o.Power>c.Power)).Select(o=>o.CardId.ToString()).ToArray():Array.Empty<string>()});
File.WriteAllText(Path.Combine(output,"card-audit.json"),JsonSerializer.Serialize(audit,options));
File.WriteAllLines(Path.Combine(output,"card-coverage.md"), new[] {
    "# Cobertura del texto de cartas WZ1", "", "Esta tabla comprueba programas compilados, no certifica todas las keywords ni el equilibrio. Los Héroes y Clases tienen identidad/estadísticas, pero no sus pasivas. Solo Guardián y Único están habilitados como reglas generales de keywords.", "",
    "| ID | Carta | Tipo | Texto de efecto | Keywords impresas |", "| --- | --- | --- | --- | --- |"
}.Concat(catalog.All.Select(c => "| " + c.CardId + " | " + c.Name + " | " + c.Type + " | "
    + (EffectEngine.Supports(c) || AdvancedEffects.Supports(c) || ExpandedEffects.Supports(c) ? "Compilado" : c.EffectText == "Sin habilidades." ? "Sin habilidades"
        : c.CardId.Value == "WZ1-042" && c.EffectText == "Puede interceptar desde Retaguardia." && c.Keywords.Contains("Guardián") ? "Cubierto por Guardián" : "Pendiente")
    + " | " + string.Join(", ",c.Keywords) + " |")));
Console.WriteLine("Diagnostics PASS; 50 legacy + 2 adversarial matches are separate from the 100 paired games.");

static (MatchRow Row, List<string> Trace) Simulate(ulong seed, DeckList d0, DeckList d1, CardCatalog catalog, RuleConfig rules)
{
    var session = new GameSession(catalog);
    Require(session.Start(new MatchSetup(seed, rules, catalog.ContentVersion, new PlayerSetup(PlayerId.Player0,d0), new PlayerSetup(PlayerId.Player1,d1))));
    var trace = new List<string>();
    foreach (var id in new[]{PlayerId.Player0,PlayerId.Player1})
    {
        // Only own hand inspected; mulligan whole hand when no turn-one/two play.
        var affordable = session.State.GetPlayer(id).HandInstanceIds.Any(i=> Definition(session.State,catalog,i).CostPa <= 2);
        Require(session.ConfirmMulligan(id,affordable?MulliganDecision.KeepHand:MulliganDecision.ReplaceWholeHand));
        trace.Add("Mulligan:"+id+":"+affordable);
    }
    var commands=3; var plays=0; var attacks=0; var heroAttacks=0; var reorgs=0; var rearPlays=0; var intercepts=0;
    while (session.State.Phase != MatchPhase.GameOver)
    {
        if (commands>=2000 || session.State.Turn>150) throw new Exception($"Incomplete match seed={seed} decks={d0.DeckId}/{d1.DeckId}");
        var legal=LegalActionQuery.For(session);
        if (legal.Count==0) throw new Exception("No legal action before GameOver "+seed);
        // Stable input order breaks ties. Public board + acting player's hand only.
        var action=legal.Select((a,i)=>(a,i,score:Score(session.State,catalog,a))).OrderByDescending(x=>x.score).ThenBy(x=>x.i).First().a;
        trace.Add($"{session.State.Turn}:{action.ActorId}:{action.Type}:{action.PrimaryInstanceId}:{action.SecondaryInstanceId}:{action.TargetRow}:{action.TargetSlotIndex}:{action.TargetsHero}:{action.ReorganizeMode}");
        Require(Submit(session,action));
        GameStateInvariants.AssertValid(session.State);
        commands++;
        if(action.Type==CommandType.Play){plays++; if(action.TargetRow==BoardRow.Rearguard && action.SecondaryInstanceId.IsNone)rearPlays++;}
        if(action.Type==CommandType.Attack){attacks++; if(action.TargetsHero)heroAttacks++;}
        if(action.Type==CommandType.Reorganize)reorgs++;
        if(action.Type==CommandType.ChooseTargets || action.Type==CommandType.PassPriority)intercepts++;
    }
    if(!session.State.Result.IsFinished || !session.State.Result.HasWinner)throw new Exception("Invalid terminal state");
    var row=new MatchRow(seed,d0.DeckId,d1.DeckId,session.State.FirstPlayerId.Index,session.State.Result.Winner.Index,session.State.Turn,commands,plays,attacks,heroAttacks,reorgs,rearPlays,session.State.EventLog.Count(e=>e.Type==GameEventType.AttackIntercepted),session.State.EventLog.Count(e=>e.Type==GameEventType.DrawFailed),session.AcceptedCommands.Last().Type==CommandType.EndTurn?"failed-draw":"combat",session.State.Result.Cause.ToString(),GameStateHasher.Hash(session.State),session.State.EventLog.Count(e=>e.Type==GameEventType.ActionPlayed),
        session.State.EventLog.Count(e=>e.Type==GameEventType.ReactionPlayed),session.State.EventLog.Count(e=>e.Type==GameEventType.ResponseUsed),session.State.EventLog.Count(e=>e.Type==GameEventType.EquipmentAttached),session.State.EventLog.Count(e=>e.Type==GameEventType.TriggerQueued),session.AcceptedCommands.Count(c=>c.Type==CommandType.ChooseTargets));
    return (row,trace);
}

static double Score(GameState state, ICardCatalog catalog, LegalAction a)
{
    if(a.Type==CommandType.EndTurn)return 0;
    if(a.Type==CommandType.PassPriority)return 1;
    if(a.Type==CommandType.ChooseTargets)return 2;
    if(a.Type==CommandType.React)return 10;
    if(a.Type==CommandType.Respond)return 5;
    if(a.Type==CommandType.Equip)return 30;
    var self=state.GetPlayer(a.ActorId);
    if(a.Type==CommandType.Play)
    {
        var c=Definition(state,catalog,a.PrimaryInstanceId); var p=c.Power??0;
        if (c.Type == CardType.Action)
        {
            if (AdvancedEffects.Supports(c)) return c.Effects[0].Opcode == EffectOpcode.OrderDrawBottom ? 85 : 1;
            var target = state.Cards[a.SecondaryInstanceId];
            // Never inspect hidden cards. Prefer buffs to friendly attackers, debuffs to enemies.
            var friendly = target.ControllerId == a.ActorId;
            var delta = c.Effects.Where(e => e.Opcode == EffectOpcode.PowerUntilEndTurn).Sum(e => e.Amount);
            return (delta > 0 && friendly || delta < 0 && !friendly) ? 85 : -1;
        }
        if(!a.SecondaryInstanceId.IsNone)
        {
            var old=Definition(state,catalog,a.SecondaryInstanceId).Power??0;
            return p>old ? 10+(p-old)*3 : -1;
        }
        // Stage in rear only when front is full; this is not a special forced test action.
        return 15+p*2+(a.TargetRow==BoardRow.Vanguard?8:0);
    }
    if(a.Type==CommandType.Attack)
    {
        var p=Definition(state,catalog,a.PrimaryInstanceId).Power??0;
        var other=state.GetPlayer(a.ActorId==PlayerId.Player0?PlayerId.Player1:PlayerId.Player0);
        var q=Definition(state,catalog,a.TargetsHero?other.HeroInstanceId:a.SecondaryInstanceId).Power??0;
        if(p<q)return -1;
        if(a.TargetsHero)return other.Resolution==1?1000:100+p-q;
        return p>q?80+q-(p-q)*0.1:45+q;
    }
    if(a.Type==CommandType.Reorganize)
    {
        // Only one free reorganization: avoid loops and do not burn PA without a tactical gain.
        if(self.FreeReorganizationUsed)return -1;
        state.TryGetCard(a.PrimaryInstanceId,out var first);
        if(first.Zone!=CardZone.Rearguard)return -1;
        var p=Definition(state,catalog,a.PrimaryInstanceId).Power??0;
        if(a.ReorganizeMode==ReorganizeMode.SwapRows)
        {
            state.TryGetCard(a.SecondaryInstanceId,out var second);
            if(second.Zone!=CardZone.Vanguard)return -1;
            var q=Definition(state,catalog,a.SecondaryInstanceId).Power??0;
            return p>q ? 65+p-q : -1;
        }
        return a.TargetRow==BoardRow.Vanguard?70+p:-1;
    }
    return -1;
}
static CardDefinition Definition(GameState s, ICardCatalog c, CardInstanceId id)
{
    if(!s.TryGetCard(id,out var card)||!c.TryGet(card.DefinitionId,out var definition))throw new Exception("Missing card "+id);
    return definition;
}
static void Require(CommandResult r){if(!r.Accepted)throw new Exception("Rejected command: "+r.RejectionReason);}
static CommandResult Submit(GameSession s, LegalAction a)=>a.Type switch
{
    CommandType.Play=>!a.HasBoardSlotTarget?s.PlayAction(a.ActorId,a.PrimaryInstanceId,a.SecondaryInstanceId):s.Play(a.ActorId,a.PrimaryInstanceId,a.TargetRow,a.TargetSlotIndex),
    CommandType.Equip=>s.Equip(a.ActorId,a.PrimaryInstanceId,a.SecondaryInstanceId),
    CommandType.React=>s.SubmitEffectOption(a),
    CommandType.Respond=>s.SubmitEffectOption(a),
    CommandType.Attack=>a.TargetsHero?s.AttackHero(a.ActorId,a.PrimaryInstanceId):s.Attack(a.ActorId,a.PrimaryInstanceId,a.SecondaryInstanceId),
    CommandType.Reorganize=>a.ReorganizeMode==ReorganizeMode.SwapRows?s.ReorganizeSwap(a.ActorId,a.PrimaryInstanceId,a.SecondaryInstanceId):s.ReorganizeMove(a.ActorId,a.PrimaryInstanceId,a.TargetRow,a.TargetSlotIndex),
    CommandType.ChooseTargets=>s.ChooseInterceptor(a.ActorId,a.PrimaryInstanceId),
    CommandType.PassPriority=>s.DeclineInterception(a.ActorId),
    CommandType.EndTurn=>s.EndTurn(a.ActorId),
    _=>throw new Exception("Unhandled legal action "+a.Type)
};
record MatchRow(ulong Seed,string Deck0,string Deck1,int FirstPlayer,int Winner,int Turns,int Commands,int Plays,int Attacks,int HeroAttacks,int Reorganizations,int RearPlays,int Interceptions,int FailedDraws,string FinalBlow,string Cause,string Hash,int ActionsResolved,int ReactionsPlayed,int ResponsesUsed,int EquipmentAttached,int TriggeredEffects,int ChoicesMade);
