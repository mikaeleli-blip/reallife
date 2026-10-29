using System.Collections.Generic;
using System.Linq;
using RealLife.Game.UI;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>The market panel: what the town spends elsewhere for want of each kind of business, and where the next one
    /// of each stands. It reports; it doesn't suggest.</summary>
    public sealed partial class Hud
    {
        bool marketAll;

        void MarketPanel()
        {
            Rect area;
            var rows = BeginPanel("Market", out area);
            rows.Pair("Spent out of town last month", Town.Money(T.LastOutOfTown));
            var br = rows.Next(26, 6);
            var bt = new Rect(br.x, br.y, 120 * Ui.S, br.height);
            if (Ui.Button(bt, "Show all", marketAll)) marketAll = !marketAll;
            Tip(bt, "Also list the ones nothing much is going elsewhere for.");
            var list = T.Prospects;
            if (list == null || list.Count == 0)
            {
                Ui.Small(rows.Next(20), "Filled in at the start of each month.", Ui.Dim);
                EndPanel(rows);
                return;
            }
            MarketGroup(rows, list, 0, "Shops and services", "goes elsewhere");
            MarketGroup(rows, list, 1, "Industry and farms", "brought in");
            MarketGroup(rows, list, 2, "Offices", "goes out of town");
            EndPanel(rows);
        }

        void MarketGroup(Rows rows, List<Prospect> list, int g, string title, string amount)
        {
            var all = list.Where(p => p.Group == g).ToList();
            var later = all.Where(p => p.State == Outlook.TooSmall && (marketAll || p.Away >= 100 || Defs.Get(p.Kind).Chain != null)).OrderBy(p => p.MinPop).ToList();
            var rest = all.Where(p => p.State != Outlook.TooSmall).ToList();
            var shown = rest.Where(p => marketAll || Notable(p)).OrderBy(p => p.State == Outlook.Opening || p.State == Outlook.Building ? 0 : 1)
                            .ThenByDescending(p => p.Away).ToList();
            rows.Header(title);
            if (shown.Count > 0)
            {
                var hr = rows.Next(16, 2);
                if (amount != null) Ui.Right(new Rect(hr.x, hr.y, hr.width * 0.62f, hr.height), "<size=" + Mathf.RoundToInt(11 * Ui.S) + ">" + amount + " a month</size>", Ui.Dim);
                Ui.Right(hr, "<size=" + Mathf.RoundToInt(11 * Ui.S) + ">the next one</size>", Ui.Dim);
            }
            foreach (var p in shown) MarketRow(rows, p);
            int hidden = rest.Count - shown.Count;
            if (hidden > 0) Ui.Small(rows.Next(18, 2), hidden + " more with little or nothing " + (g == 1 ? "brought in" : "going elsewhere"), Ui.Dim);
            if (later.Count > 0)
            {
                string s = Ui.Col("With more people: ", Ui.Dim) + string.Join(Ui.Col(" · ", Ui.Dim), later.Select(p => KindLabel(p) + Ui.Col(" " + p.MinPop.ToString("#,0"), Ui.Dim)));
                var r = rows.Peek(10);
                float h = Ui.Wrap(r, "<size=" + Mathf.RoundToInt(12 * Ui.S) + ">" + s + "</size>");
                rows.Space(h / Ui.S + 4);
            }
        }

        /// <summary>Worth a line without Show all: something's happening, or money is going elsewhere for want of it.</summary>
        static bool Notable(Prospect p)
        {
            switch (p.State)
            {
                case Outlook.Opening: case Outlook.Building: case Outlook.NoLot: case Outlook.NoBacker: case Outlook.BuildersBusy: case Outlook.Failed: return true;
                case Outlook.Promising: if (p.Of > 0) return true; break;
            }
            return p.Group == 2 || p.Away >= 100;
        }

        static string KindLabel(Prospect p)
        {
            var n = Defs.Get(p.Kind).Name;
            return p.Group == 0 && p.Name != n && Defs.Get(p.Kind).Chain == null ? n : p.Name;
        }

        void MarketRow(Rows rows, Prospect p)
        {
            var r = rows.Next(20, 1);
            float w = r.width;
            string label = p.Name + (p.Here > 0 ? Ui.Col("  ×" + p.Here, Ui.Dim) : "");
            var nameR = new Rect(r.x, r.y, w * 0.46f, r.height);
            var site = p.Site >= 0 ? T.GetBuilding(p.Site) : null;
            if (site != null) { if (Ui.Link(nameR, label)) gc.FocusBuilding(site); }
            else Ui.Small(nameR, label);
            Ui.Right(new Rect(r.x, r.y, w * 0.62f, r.height), p.Away >= 1 && !p.Estimate ? Town.Money(p.Away) : "–", Ui.Dim);
            string tip; Color c;
            string st = Standing(p, out tip, out c);
            Ui.Right(r, st, c);
            Tip(r, tip);
        }

        /// <summary>A rough figure: $5.2k, $32k, $1.4M.</summary>
        static string Approx(double v)
        {
            if (v >= 1e6) return "$" + (v / 1e6).ToString("0.0") + "M";
            if (v >= 1e4) return "$" + (v / 1e3).ToString("0") + "k";
            if (v >= 1e3) return "$" + (v / 1e3).ToString("0.0") + "k";
            return "$" + (v / 100).ToString("0") + "00";
        }

        static string A(string noun) { return ("aeiou".IndexOf(char.ToLower(noun[0])) >= 0 ? "an " : "a ") + noun; }

        /// <summary>A few words on where the next one stands, and a sentence or two more for the tooltip.</summary>
        static string Standing(Prospect p, out string tip, out Color c)
        {
            var d = Defs.Get(p.Kind);
            string kind = d.Chain ?? (d.HeadOf.HasValue ? d.Name : d.Name.ToLower() + (d.Name.EndsWith("repair") ? " shop" : ""));
            string one = d.HeadOf.HasValue ? "its head office" : d.Chain != null ? (p.Here > 0 ? "another " + d.Chain + " store" : d.Chain) : (p.Here > 0 ? "another " + kind : A(kind));
            string One = char.ToUpper(one[0]) + one.Substring(1);
            bool says = p.State == Outlook.Thin || p.State == Outlook.Promising || p.State == Outlook.NoLot || p.State == Outlook.NoBacker || p.State == Outlook.BuildersBusy || p.State == Outlook.Failed;
            string next = says && p.Group == 0 && p.Name != d.Name && d.Chain == null ? "Next would be " + A(kind) + ". " : "";
            string away = p.Away < 1 ? "" : p.Estimate ? "It would take about " + Town.Money(p.Away) + " a month of the town's grocery shopping. "
                        : p.Group == 2 ? (p.Here == 0 ? Town.Money(p.Away) + " a month of the town's office work goes to firms out of town. "
                                                      : Town.Money(p.Away) + " a month of work the office" + (p.Here > 1 ? "s" : "") + " here couldn't take on went out of town. ")
                        : p.Group == 1 ? Town.Money(p.Away) + " a month of what it makes is brought in from outside. "
                        : p.Here == 0 ? Town.Money(p.Away) + " a month goes out of town, or unspent, for want of one. "
                        : Town.Money(p.Away) + " a month of custom was turned away at the one" + (p.Here > 1 ? "s" : "") + " here. ";
            string here = p.Here == 1 ? "the one" : "the " + p.Here;
            c = Ui.Dim;
            switch (p.State)
            {
                case Outlook.Opening:
                    c = Ui.Good; tip = "Someone is starting " + one + " this month."; return "opening";
                case Outlook.Building:
                    c = Ui.Good; tip = One + " is going up. Click the name to see it."; return "being built";
                case Outlook.Promising:
                    c = Ui.Good;
                    if (p.Of > 0)
                    {
                        tip = next + away + "The numbers for " + one + " have added up " + p.Have + (p.Have == 1 ? " month" : " months") + " running; someone starts one if they still do after " + p.Of + ".";
                        return "promising " + p.Have + "/" + p.Of;
                    }
                    tip = next + away + One + " would pay its way.";
                    return "could open";
                case Outlook.NoLot:
                    c = Ui.Warn;
                    if (d.HeadOf.HasValue)
                        tip = away + Defs.Get(d.HeadOf.Value).Chain + (p.Here > 0 ? "'s stores need a head office to run them" : " would come, but it needs a head office downtown")
                              + ", and there's no free 1×1 lot zoned Shops by a road for one (that isn't part of a lot big enough for a store).";
                    else if (d.Chain != null && p.Here > 0)
                        tip = away + One + " would open, but there's no free " + d.W + "×" + d.H + " lot zoned Shops by a road in a part of town its stores are far from.";
                    else tip = next + away + One + " would open, but there's no free " + d.W + "×" + d.H + " lot zoned " + GameController.ZoneName(d.Zone) + " by a road on the highway's network.";
                    return "no " + d.W + "×" + d.H + " " + GameController.ZoneName(d.Zone).ToLower() + " lot";
                case Outlook.NoBacker:
                    c = Ui.Warn;
                    if (d.HeadOf.HasValue) tip = away + "The family behind " + Defs.Get(d.HeadOf.Value).Chain + " hasn't the " + Town.Money(Town.FoundersStake(p.Kind)) + " or so it takes to set up a head office yet.";
                    else if (d.Chain != null && p.Here > 0) tip = away + "The town could do with " + one + ", but its head office hasn't the money for it yet.";
                    else if (d.Chain != null) tip = away + "No family here has the " + Town.Money(Town.FoundersStake(p.Kind) + Town.FoundersStake(d.Office.Value)) + " or so it takes to found " + d.Chain + ": its head office and its first store.";
                    else tip = next + away + One + " would pay its way, but no family here has the " + Town.Money(Town.FoundersStake(p.Kind)) + " or so it takes to start one.";
                    return "nobody can fund it";
                case Outlook.BuildersBusy:
                    c = Ui.Warn;
                    tip = next + away + One + " would open, but the builders here have taken on all they can for now.";
                    return "builders busy";
                case Outlook.Queued:
                    tip = next + away + One + " could open, but another " + (p.Group == 2 ? "office" : "plant") + " is going up first.";
                    return "waiting its turn";
                case Outlook.Failed:
                    tip = next + away + "One went under here lately. Nobody tries again before " + Town.DateOf(p.Until) + " unless the case is strong.";
                    return "failed lately";
                case Outlook.TooSmall:
                    tip = away + "Comes with a town of about " + p.MinPop.ToString("#,0") + " people.";
                    return "from " + p.MinPop.ToString("#,0") + " people";
                case Outlook.Thin:
                    string with = d.Chain != null && p.Here == 0 ? " (a store and its head office)" : "";
                    tip = next + away + (p.Need > 0 ? One + with + " would need about " + Approx(p.Need) + " a month to pay its way." : "Not enough for " + one + with + " to pay its way.");
                    return p.Need > 0 ? "needs ~" + Approx(p.Need) : "not enough yet";
                case Outlook.HaveRoom:
                    tip = next + away + (p.Group == 2 ? (p.Here == 1 ? "The one here" : "The ones here") + " can still take on more staff and work."
                                                      : (p.Here == 1 ? "The one here isn't" : "The ones here aren't") + " full or turning many people away yet.");
                    return "room at " + here + " here";
                case Outlook.Struggling:
                    tip = next + away + "Nobody opens " + one + " while one here is losing money" + (p.Group == 1 ? ", short of staff or barely breaking even." : ".");
                    return "one here struggling";
                case Outlook.Enough:
                    tip = next + away + (p.Per > 0 ? "As many as the town keeps for now: about one per " + p.Per.ToString("#,0") + " people" + (p.Group == 1 && p.Per == 500 ? " making the same thing" : "") + "." : "One is enough for now.");
                    return "enough for now";
                case Outlook.NoStaff:
                    tip = next + away + (p.Of < 0
                        ? "A new plant opens when about " + p.Need.ToString("0") + " people have been looking for work (or driving out of town for low pay) for a couple of months; " + p.Have + " now."
                        : "It takes about " + p.Need.ToString("0") + " people looking for work who could do the jobs (or driving out of town for low pay); " + p.Have + " now.");
                    return "short of workers " + p.Have + "/" + p.Need.ToString("0");
                case Outlook.Served:
                    tip = away + "Nobody opens " + one + " while the town's other grocers can still take everyone in.";
                    return "town's served";
                case Outlook.WontPay:
                    tip = next + away + "At today's prices, wages and what its inputs cost, " + one + " wouldn't cover its costs.";
                    return "wouldn't pay";
            }
            tip = "";
            return "";
        }
    }
}
