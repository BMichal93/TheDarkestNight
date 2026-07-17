# The Darkest Night — Lore Text Review

All the player-facing prose I wrote or re-themed, grouped by system. Settlement
encounters come first, in full. Tone tags mirror the in-game message colour — the
emotional read each line is meant to land:

- **[GOOD]** = GoodColor · **[GRIM]** = BadColor · **[COIN]** = GoldColor · **[FLAT]** = DimColor

`{braces}` mark runtime variables.

**Contents**
1. [Six new settlement encounters](#part-1--six-new-settlement-encounters) — written from scratch
2. [Re-themed settlement encounters](#part-2--re-themed-settlement-encounters)
3. [Re-themed world & map events](#part-3--re-themed-world--map-events)
4. [Re-themed questlines](#part-4--re-themed-questlines)

---

## Part 1 — Six new settlement encounters

Entirely new, native to this world. Each is an immediate choice with a deferred,
unexpected payoff fired days or weeks later. File: `SettlementEncounters.NewEncounters.cs`.

### 01 · The Painted Door — `E_WardSeller`
*Town · on enter · 40–70 day cooldown*

**Prompt**
> A man works door to door with a pot of charcoal and salt paste, painting a crude ward above each lintel that will have him. He says it keeps the Night from crossing a threshold. He says it with the flat conviction of a man who has said it many times and stopped caring whether it was believed, only whether it was bought.

**Choices**
- **a** — Buy a bundle for your own doors and packs. *(A few coins for a folk charm. It may be nothing. It may not.)*
- **b** — Denounce him — he preys on frightened people for coin. *(You say it loudly enough for the street to hear.)*
- **c** — Walk on. Not your business.

**Immediate**
- **[FLAT] a** — He paints the ward over your door in three quick strokes, mutters something under his breath, and moves to the next house before you've finished counting the coins into his hand.
- **[GRIM] b** — You say it loud enough for the street to hear: a fraud, preying on the frightened. A few people laugh. He does not argue. He gathers his pot and his brushes and walks away faster than a man with nothing to hide usually does.
- **[FLAT] c** — You walk on. Behind you, another door gets its painted ward, another coin changes hands. The street looks no different than it did a minute ago.

**Deferred**
- **[GRIM]** *(Bought · 50%)* — Word reaches you from a hamlet down the road: they painted their doors on your recommendation. The wards did nothing. The Night came through anyway, the way it always does when salt and charcoal are the only thing standing in front of it. You did not sell them the charm. You only vouched for the man who did. That distinction feels smaller than it should.
- **[GOOD]** *(Bought · 50%)* — Something finds your camp at the edge of night and does not come in. Whether the painted lines on your wagon actually held it, or it simply passed you by for reasons of its own, you cannot say. Your men are certain it was the ward. You let them believe it. Certainty is worth more than accuracy, some nights.
- **[GRIM]** *(Denounced — "What the Coin Was For")* — A woman stops you in a market two towns over. She recognises you from the street where you called the ward-seller a fraud. She tells you, without heat, that he was her husband — that the coin from his pots and brushes fed three children after the fever took his hands for anything finer. She does not ask for anything. She wanted you to know what the mob you started actually cost.
  - → *There is nothing adequate to say to that. You give her what coin you can spare, which does not feel like an answer, and she takes it because three children do not care whether it is an answer.*

### 02 · One Watch — `E_NightwatchShort`
*Town · on leave · dusk · clan tier ≥ 1*

**Prompt**
> The gate wardens are a man short for tonight's watch, with dusk already thickening at the tree line. The sergeant asks it plainly, not as an order — you owe the town nothing — but the wall goes unwalked at one point tonight unless someone fills it.

**Choices**
- **a** — Stand the watch yourself. *(One-Handed check — Hold the wall through the dark hours.)*
- **b** — Post one of your soldiers in your place. *(You lose their sword arm for the night, whatever the night brings.)*
- **c** — You have your own road. Refuse.

**Immediate**
- **[GOOD] a ✓** — You stand the gap until the second bell. Something tests the wall once, near the fourth hour — a scraping, low sound that stops when you move toward it. It does not come back. Dawn finds you tired and the wall unbroken.
- **[GRIM] a ✗** — You hold the gap, but not cleanly — something gets close enough to leave a mark before the sergeant's whistle turns three spears on it and it withdraws. You bleed for the town's wall tonight. The wall holds anyway.
- **[FLAT] b** — You post one of yours in the gap and go find a bed. The night passes the way nights are supposed to — you hear nothing, and in the morning your soldier is at the gate, stiff and cold-handed and unremarkable.
- **[FLAT] c** — You ride on before dusk finishes settling. The sergeant says nothing. He will fill the gap himself, or he won't, and either way it is no longer yours to answer for.

**Deferred**
- **[GOOD]** *(Stood the watch · +renown)* — The gate captain names you to his lord: the sellsword who stood a stranger's wall through the dark hours and asked nothing for it. It is a small story. It travels anyway — small stories about people who stayed are rarer than the ones about people who fought.
- **[GRIM]** *(Posted a soldier · 35%)* — Word reaches your company: the soldier you posted in the gap that night did not survive the following week's dusk raid, out on a separate patrol. Nobody connects the two nights. You do. You posted them in a gap once because it cost you less than standing it yourself, and the accounting for that never quite closes.
- **[FLAT]** *(Posted a soldier · else)* — Your soldier mentions, weeks later and without making anything of it, that the watch was quiet and they'd do it again. You did not ask them how they felt about being sent in your place. They did not seem to need you to.
- **[GRIM]** *(Refused · 40%)* — The section of wall that went unwalked the night you rode past broke before dawn — not badly, a handful of dead, but the town remembers who had the strength to fill the gap and rode on instead. Word of it reaches the settlement's lord before you do.
- **[FLAT]** *(Refused · else)* — Nothing came of the gap you left unfilled. The sergeant covered it himself, or got lucky, or both. You will never know which, and the town has already forgotten you were asked.

### 03 · The Vial Trade — `E_BloodBroker`
*Town · on enter · Bloodbound culture or mage-eligible*

**Prompt**
> A Bloodbound broker finds you at the market with the unhurried manner of someone who has done this many times and never once been refused without eventually being sought out again. He wants Demon Blood — yours, if you carry it, or fresh, if you are willing to let him bleed a prisoner in your presence. He pays well either way. He is entirely untroubled by the question of where it goes after it leaves his hands.

**Choices**
- **a** — Sell what you carry. *(He pays fairly. What he does with it afterward is his business, not yours.)*
- **b** — Let him bleed a prisoner. *(More coin, less clean. Requires a prisoner in your roster.)*
- **c** — Refuse the whole trade.

**Immediate**
- **[COIN] a** — He decants the vials into a lined case without spilling a drop and counts the coin into your palm without being asked twice.
- **[GRIM] b** — He works quickly and without ceremony, and the prisoner is more frightened by his calm than by the blade. You are paid well. You do not watch the whole thing.
- **[FLAT] c** — You refuse the whole trade. He accepts it the way he accepts everything — without argument, filing you under a different heading than the one he'd hoped for.

**Deferred**
- **[GRIM]** *(Sold vials · ambush)* — The vials you sold fed a rite that slipped whatever circle was meant to hold it. Something carrying your scent finds your column at dusk — the broker's rite went wrong, or right in a way nobody warned you about, and either way the blood remembered where it came from.
- **[GOOD]** *(Bled a prisoner · obligation)* — The broker's guild remembers a useful, uncomplaining partner. A courier leaves a case of Demon Blood at your camp with no note attached — and the standing, unspoken expectation that you will be just as useful the next time they ask.

### 04 · Born at the Turning — `E_DuskbornChild`
*Village · on enter · long deferral (60–90 days)*

**Prompt**
> A midwife shows you a newborn, delivered at the exact moment dusk crossed into full dark — the village is already calling the child demon-marked and wants it given back to the Night before it can grow into whatever it was born as. The mother says nothing. She is past arguing and not yet past hoping someone else will.

**Choices**
- **a** — Take the child into your protection. *(One more mouth to feed on a hard road.)*
- **b** — Pay the village to shelter it properly. *(Coin enough to buy the child a chance at an ordinary childhood.)*
- **c** — Leave it to their judgment.

**Immediate**
- **[GOOD] a** — You take the child. Your men grumble about the extra mouth for exactly one evening and then stop, the way soldiers do about children they've decided to like.
- **[GOOD] b** — You pay enough that the village has no more excuse to be afraid of a hungry mouth than any other. The midwife takes the coin without meeting your eyes, which you decide to read as gratitude rather than shame.
- **[FLAT] c** — You leave it to their judgment and ride on before you can learn what that judgment was. You tell yourself it is not yours to carry. You do not entirely believe it.

**Deferred**
- **[GOOD]** *(Saved · 85%)* — Months compress into a single report reaching you on the road: the duskborn child is simply a child, quick-eyed and unremarkable except for a habit of tapping rhythms against tables that no one taught them — the kind of habit a Spellbook scribe would recognise instantly and everyone else mistakes for restlessness. Someone should teach them, eventually. You make a note of it.
- **[GRIM]** *(Saved · 15%)* — The village's fear was not entirely wrong after all. Whatever the child drew toward it at the moment of its birth has finally come looking — years late, patient the way the Night is patient. You deal with it, but you understand now why they wanted it given back.
- **[GRIM]** *(Left to judgment)* — Whatever the village decided to do with the child, it was not gentle, and word of it has reached the surrounding hamlets. They do not tell you outright what happened. The way people in that region look at you now tells you enough.

### 05 · The Family That Would Not Open — `E_SaltCircle`
*Village · on leave · dusk*

**Prompt**
> A shuttered croft at the village edge, salt laid thick across the threshold. A family huddles inside, convinced — loudly, through the door — that you are a demon wearing a borrowed face. Dusk is closing fast and your column needs the shelter more than it needs an argument.

**Choices**
- **a** — Talk them down. *(Charm check — Talk them down through the door.)*
- **b** — Break the door. You need the shelter. *(They will scatter into the Night rather than let you in.)*
- **c** — Leave food on the step and go.

**Immediate**
- **[GOOD] a ✓** — You talk to them through the door for a long time, low and unhurried, until the bolt finally slides back on its own. They do not apologise. They do not need to. You did not need the shelter as badly as you needed them to see that fear was the only thing behind that door.
- **[FLAT] a ✗** — You talk to them through the door and nothing you say reaches past the salt line. Eventually you give up and make camp outside their walls instead. They watch you through a gap in the shutters until you're asleep, or pretend to be.
- **[GRIM] b** — You break the door. The family scatters out the back and into the dark rather than stay under a roof with what they believe you are. You have the shelter. You do not have the family, and you do not know yet what the Night made of that choice.
- **[GOOD] c** — You leave what food you can spare on the step, in plain sight of the gap in the shutters, and make camp a respectful distance off. Nobody opens the door. Somebody, eventually, takes the food.

**Deferred**
- **[GOOD]** *(Left food)* — Weeks on, a jar of preserves turns up at your camp, left by a courier who won't say who sent it or how they found you. A child in that family, you hear later, has taken to calling themselves after you. The fire you didn't have to spend was, it turns out, the entire point.
- **[GRIM]** *(Broke the door)* — The rumour that you are demon-touched has outrun you along the road — a family scattered into the dark rather than share a roof with you, and that story travels faster and stranger with every retelling. A Temple prelate two towns over has started asking pointed questions about your movements.
- **[FLAT]** *(Talked them down)* — Nothing further comes of the family behind the salt-laid door. That is, on reflection, the whole point of the choice you made — the fire you didn't have to spend was worth exactly as much as it cost you, which was nothing at all.

### 06 · The Bell of a Nameless Town — `E_MusterBell`
*Town · on enter · ownerless city-state only*

**Prompt**
> {Town} answers to no kingdom, and its muster bell is ringing — an ownerless city-state with no banner to call for help, begging any armed company passing through to hold the wall for one more night. The captain who meets you at the gate has the specific exhaustion of someone who has asked this question of travelers before and been refused more often than not.

**Choices**
- **a** — Help hold the wall. No price named. *(A real fight, offered freely.)*
- **b** — Name your price first, then help. *(Coin up front. It costs you something else.)*
- **c** — Decline. It is not your wall.

**Immediate**
- **[GOOD] a (hurt)** — You hold the wall through the worst of it and come away marked for the trouble — but the wall holds, and the captain does not forget who stood on it without being asked what it was worth.
- **[GOOD] a (clean)** — You hold the wall through a night that never quite breaks into the disaster it threatens to be. The captain watches you work and says nothing, which from him is evidently high praise.
- **[COIN] b** — You name a price before you draw a blade. The captain pays it without much grace, and you hold the wall regardless — the coin bought your presence, not your effort, and you gave both anyway.
- **[FLAT] c** — You decline. The bell keeps ringing behind you as you ride, calling for a company that isn't yours to be.

**Deferred**
- **[GOOD]** *(Helped freely)* — {Town} opens its gates to you for good. Its captain's name, when you need a favor called in, turns out to be worth exactly what he implied it was.
- **[FLAT]** *(Named a price)* — The story of the sellsword who haggled while the wall of {Town} nearly fell has made its way to the next town over, told two different ways by two different people who were both there. They lived. That part of the story is somehow always mentioned second.
- **[GRIM]** *(Declined · 40%)* — The wall of {Town} did not hold that night. Its refugees are on the roads now, and some of them remember exactly which company rode past the muster bell without answering it.
- **[FLAT]** *(Declined · else)* — {Town} held its own wall without you, in the end. Nobody there remembers you rode past — they had a longer night to remember instead.

---

## Part 2 — Re-themed settlement encounters

Existing baseline encounters rewritten so the antagonist is the demon-cult and the
fantasy is the Spellbook / the Night. These are the passages I changed; each is one
moment inside a larger encounter. Files: `SettlementEncounters.*.cs`.

**The healed girl — messenger's accusation** *(revised: was "your kind / follow the fire")*
> A messenger reaches you on the road — rough-spoken, half-panicked, sent by the same village. The girl you healed is gone. Taken in the night by figures in grey cloaks. The mother sent word not as a plea but as an accusation: she knows what you are now — someone who can work the symbols — and she knows the demon-cult hunt for exactly that. She says you painted a target on her daughter's forehead the moment you drew the mark that healed her.

**The young mage — "does it have to end that way"**
> He has heard what the demon-cult do to people like him. He wants to know if it has to end that way.

- Resolutions: *"A week later, word reaches you from the north: a young mage was seen leading a small band of volunteers against a demon-cult raiding column…"* — or — *"A week later, the demon-cult gain a recruit."*

**The cold marker — object left in a captured keep**
> In a room off the keep's great hall, placed on a shelf between two books as if it belonged there: a small object of grey stone that is cold in a way that has nothing to do with temperature. The demon-cult put this here before the siege began — possibly years before. It is a marker. It means: we were here. We will return for it.

- Take it: *"You wrap it in cloth and keep it separate from everything else. It will be cold to the touch for as long as you carry it. The demon-cult use these to locate each other across distances. You now own a gap in their network…"*
- Leave it: *"You leave it exactly where it is, touching nothing. When the demon-cult return — and they will return — they will find the keep changed but the marker undisturbed. They will conclude their absence was unnoticed. You will know they concluded that. That is a small and specific advantage."*

**The mage who tracked the marker** *(revised: was "A fire-mage")*
> A mage finds you on the road — young, precise, her hands still chalk-stained from a hundred practice-formulas, clearly following a thread she picked up some time ago. She was tracking a demon-cult marker that has gone silent. She knew what it was. She knows you destroyed it. She does not thank you with words. She tells you something about where she found the thread's other end: a direction, a name, a piece of the network you did not have before.

**The undisturbed marker holds — deception pays off**
> Word reaches you: cult scouts entered {Settlement} two nights ago and departed before dawn. The garrison commander reports they went directly to one room and left without searching further — they found the marker undisturbed and concluded their absence went unnoticed. Your deception holds. The garrison commander, who trusted you with this intelligence, is now more inclined to trust you with others.

**The man with something in his pocket**
> His hands moved once — toward his left pocket when you said 'visitors', then caught themselves. He was given something to keep and told to say nothing. You do not confront him. You wait until he uses the privy and check the pocket: a folded note with a demon-cult symbol and a date three days from now. He was given a message to hold, not just a cover story. The date is a meeting.

**The plainclothes tail — sanctioned surveillance**
> You use a shop window and a narrow passage to get a clear look without stopping. City watch — not uniformed, working plainclothes. This is a sanctioned surveillance, not a freelance tail. Someone in city administration has an official interest in your movements. That is a different kind of problem than a demon-cult watcher. You continue your route as if unaware and note everything they observe.

**The forged permit — grey courier cloth**
> The permit was forged. The wagon contained grey-dyed cloth that matches cult courier colours exactly — not contraband in any legal sense, but material with a specific use. It cleared your gate. The merchant was gone before the guard's follow-up instinct completed. You made a small decision at a gate and someone's supply run went through. Whether that matters depends on what the cloth is for.

**The lone tanner — one man laying markings**
> Your questioning finds a thread. A tanner at the edge of {Village} — not frightened, not defiant, just quietly wrong. The wrong kind of calm for someone who has seen what he has seen. You put the pieces together: one man, acting alone, laying markings in the fields at the demon-cult's instruction. He does not know what they mean. He knows what he was paid and what he was threatened with.

- Exile: *"You escort him to the village boundary and tell him what exile means in your jurisdiction… The demon-cult network loses this thread — but threads can be replaced."*
- Turn him: *"…a frightened man with divided loyalties and a specific contact in the demon-cult's local network. That is worth something."*

**The ring of demons closes — false priest**
> A ring of demons — grey-cloaked, cold-eyed — has closed around you without a sound. … The demons lower their hands. You are one of them now.

**The vial of ash-blood — the initiation offer**
> He produces it from inside his coat: a small sealed vial, dark and faintly luminescent, the liquid inside not quite settling the way liquid should. He describes the contents — ash-blood drawn from a living cult donor, three additional reagents he declines to name, prepared over a fortnight at specific temperatures. He says it will unlock something in whoever drinks it. He says the process is irreversible. He says this as though it is a recommendation.

**Battlefield salvage from the fallen cult**
> Among the fallen cult your men have found something and brought it to you because they did not know what else to do with it. You do not know what it does. You know that the demon-cult were willing to die for it.

**The blind seer who teaches a formula**
> An old man sits outside the inn, eyes clouded white. He does not look at you. He faces toward you. "You carry a shape in your hands," he says. "Not a soldier's shape. The kind that draws on the dark and gets an answer back." He taps the bench beside him.

- He teaches: *"You sit with him for an hour. He traces formulas in the dust with one finger, patterns he learned long before anyone called it a Spellbook. By the time the village lanterns are lit, something has opened in you — the shape of {spell}, given without ceremony."*
- You've outgrown him: *"…'You have gone further than I can follow.' He does not mean it as a compliment."*
- Warning: *"A door that opens both ways," he says. "Every formula you tap, something on the other side feels it too."*

**The child who copies your hands** *(revised: was born-with-the-gift "pull")*
> A girl of perhaps six stops playing and stares at your hands — not at your horse, not at your armor, but at the way your fingers still hold the shape of the last formula you tapped. She copies the motion, clumsily, watching to see whether anything answers. Nothing does; not yet, not without someone to teach her which marks mean something and which are only air. Her mother pulls her back. The girl's eyes do not leave your hands.

**The cult watcher in the shadow**
> Leaving the city, you become aware — not by sight but by the particular absence of warmth — of someone in a building's shadow cataloguing your party. Grey cloak, pale still face, the patient posture of something that is not in a hurry because it has learned not to be. A demon-cult agent is noting your movements.

- Confront: *"You turn your horse and ride toward the shadow. They are gone before you reach the doorway — not fled, simply gone, the way the demon-cult go when they choose not to be found…"*

---

## Part 3 — Re-themed world & map events

Campaign-map events rewritten for this world. Files: `CampaignMapEvents.*.cs`,
`BattleEvents.*.cs`. The strongest new piece is **The Turning** — a wholly new event.
The rest are baseline events re-skinned to the demon-cult.

**The Turning — a mage lord's blood fails to hold the Night out (NEW)**
> Something in {lord} of {clan} is losing the argument with the dark. You feel it before you see it — they do not know it yet, or they do and cannot stop it. The Night is working its way through…

- Harvest it: *"You cut it off in {lord}. For a moment there is warmth — real warmth… Fifteen days of your own strength, given back. They are nothing at all."*
- Ward it (non-harvester): *"You reach in and hold the Night back from {lord} — carefully, at a cost you feel but cannot measure."*
- Choice hint: *"You have felt the Night's pull on your own working. You know what this looks like, and you will not touch it."*

**Iron Winter / Scorching Sun — cult altars warp the weather**
> Iron Winter (cult Altar) — the cold called by the altar has descended on {kingdom}.
> Scorching Sun (cult Altar) — the heat called by the altar burns {kingdom}.

**Embers of Hope — the cult reclaims its dead**
> The demon-cult do not mourn their fallen — they call them back. / The demon-cult count their fallen, and find them present. Embers of Hope — the demon-cult hold {n} cities now.

**Darkened Roads — tribute & caravans vanish**
> Darkened Roads — {n} tribute-column(s) vanish on the steppe-roads of {kingdom}. Tribute-riders do not return. The war-camp waits for gold and grain that will not arrive. *(steppe variant)*
> Darkened Roads — {n} caravan(s) vanish on the roads of {kingdom}. Trade dies. Prosperity crumbles. *(general variant)*

**The Long March — demon columns take the field**
> The Long March — {n} great columns of demons set foot in {kingdom}.

**A Wolf in Sheep's Clothing — the tribunal names a traitor**
- Temple framing: *"{victim} of {kingdom} was denounced before the tribunal as a demon-cult sympathiser. The Inquisitor's writ arrived before they could answer the charge. Their family maintains their faith. The tribunal did not ask."*
- Bloodbound framing: *"…named before the God-King's war-council as having sold a blood-pact to the demon-cult. The God-King's word was sentence enough."*
- If you vouch and are wrong: *"Three days later, {traitor} was found at the edge of the demon-cult lands — grey-eyed and cold. The accusation was true. {ruler} has not forgotten that you spoke for them."*

**The demon-cult Gambit — the sanctuary ward & the strike at the thrones**
> The demon-cult Gambit — The sanctuary's ward blazes bright. The assassins feel it like a wall of fire…
> The demon-cult Gambit — In a single night of cold fire and silence, every Imperial throne was struck at once.

---

## Part 4 — Re-themed questlines

Main and side questlines re-pointed at the demon-cult and Aelisar Veth, the First
Emperor who shattered his soul into the cult cycle as a lock. Files:
`DragonQuestSystem.*.cs`, `BurningLabQuestSystem.*.cs`, `TempleCovenant.cs`,
`AshenQuestSystem.*.cs`, `ScholarBargainQuestSystem.*.cs`.

**Aelisar Veth — the shattered First Emperor (main quest spine)**
> It is Aelisar Veth, the First Emperor, who shattered his soul into the demon-cult cycle as a lock.
> "You are collecting what remains of me. Each cult lord you silence — …"

- Objective: *"Silence seven cult lords in battle — each one releases a shard of Aelisar."*
- Progress: *"A demon-cult lord silenced — a shard of Aelisar returns. [{count}/{target}]"*
- *"You have killed another cult lord, and the presence returns before the blood dries."*

**The ending — break the cycle, or let it turn**
- Break it: *"Spend Aelisar and your fire to break the demon-cult cycle forever. Your hero dies. The demon-cult are broken. This ends your campaign."*
- Cycle continues: *"The demon-cult still move. The cycle continues. The grey will come again…"*

**The Wasteland Rite (Burning Lab questline, re-themed)**
> The Wasteland Rite is revealed. Visit a demon-cult-owned city to consecrate it.
> An altar will rise here, as it does in all true cult cities.

- Doom ending: *"Calradia is the demon-cult's now — vast, still, perfect. The world the fires built is ended."*
- *"The grey banners lower. The cold warriors of the demon-cult march under the imperial eagle now."*

**The Temple Covenant**
> The Temple offers covenant: stand with us against the demon-cult when we call, and our rites…
> A rider in grey reaches you with word from {leader}: the Temple strikes at the demon-cult…

- Payoff: *"You ride with the templars. {n} cult warband(s) caught on the road and bloodied. …the Temple remembers."*

---

*End of review. Parts 1–2 are the settlement encounters you flagged; Parts 3–4 are the
other lore I touched. Tell me which lines want a style pass.*
