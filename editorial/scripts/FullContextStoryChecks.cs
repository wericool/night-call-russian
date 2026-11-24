using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Prompter.Runtime;

class FullContextStoryChecks {
    static void Set(object o,string name,object value) {
        o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(o,value);
    }
    static List<string> Strings(object data) {
        var res=new List<string>();foreach(object o in (IEnumerable)data)res.Add((string)o);return res;
    }
    static Dialog Load(Dictionary<string,object> row) {
        var dialog=new Dialog();var passages=new List<PassageSection>();
        foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"]) {
            var ps=new PassageSection();Set(ps,"_title",(string)p["_title"]);Set(ps,"_link",(string)p["_link"]);
            Set(ps,"_lines",Strings(p["_lines"]));Set(ps,"_variable_modifiers",Strings(p["_variable_modifiers"]));
            var choices=new List<ChoiceSection>();
            foreach(Dictionary<string,object> c in (IEnumerable)p["_choices"]) {
                var cs=new ChoiceSection();Set(cs,"_text",(string)c["_text"]);Set(cs,"_link",(string)c["_link"]);
                Set(cs,"_ending",Convert.ToBoolean(c["_ending"]));choices.Add(cs);
            }
            Set(ps,"_choices",choices);passages.Add(ps);
        }
        var vars=new List<DialogVariable>();
        foreach(Dictionary<string,object> v in (IEnumerable)row["_variables"])vars.Add(new DialogVariable((string)v["_name"],Convert.ToInt32(v["_value"])));
        Set(dialog,"_passages",passages);Set(dialog,"_variables",vars);Set(dialog,"_user_datas",Strings(row["_user_datas"]));return dialog;
    }
    static PrompterStory Start(Dialog d,string passage) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(null,passage);return story;
    }
    static List<string> Drain(PrompterStory story) {
        var lines=new List<string>();int n=0;
        while(story.can_continue) {
            if(++n>500)throw new Exception("Loop while reading dialogue");
            var line=story.GetNextLine();if(line!=null)lines.Add(line);
        }return lines;
    }
    static void Check(bool value,string message) {if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
    static bool HasText(string value,string phrase) {return value.IndexOf(phrase,StringComparison.OrdinalIgnoreCase)>=0;}
    static PrompterStory ChiaraStart(Dialog d) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Chiara",0)},"chiara-01-start");return story;
    }
    static PrompterStory SylvieStart(Dialog d) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Sylvie",0)},"sylsyl-start");return story;
    }
    static PrompterStory SeanStart(Dialog d) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Sean",0)},"sean-01-start");return story;
    }
    static PrompterStory ShinjiStart(Dialog d) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Shinji",0)},"shinji-01-start");return story;
    }
    static PrompterStory FrancineStart(Dialog d,int cat) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Crouky",cat)},"francine-start");return story;
    }
    static PrompterStory JonasStart(Dialog d) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Jonas",0),new DialogVariable("_Found.Clue.Today",0)},"jonas-start");return story;
    }
    static PrompterStory ChrisStart(Dialog d,int deal) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Deal.Carolina",deal)},"chris-01-start");return story;
    }
    static PrompterStory ShohrehStart(Dialog d) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Shohreh",0)},"shohreh-01-start");return story;
    }

    static PrompterStory AmelieStart(Dialog d,string passage,int mode) {
        var flags=new List<DialogVariable>{new DialogVariable("_freeroam",mode==3?1:0),
            new DialogVariable("_Driver.Met.Amelie",0),new DialogVariable("_Found.Clue.Today",7)};
        string[] names={"Judge","Sandman","Angel"};
        for(int i=0;i<3;i++)flags.Add(new DialogVariable("_Killer.Is."+names[i],mode==i?1:0));
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};story.StartDialog(flags,passage);return story;
    }
    static int AmelieRoutes(Dictionary<string,object> row,int mode,List<int> route) {
        var am=AmelieStart(Load(row),"amelie-01-start",mode);
        var emitted=new List<string>();am.OnActionTriggered+=(evt)=>emitted.Add(evt.action);
        var lines=Drain(am);var selected=new List<string>();
        foreach(int answer in route) {
            var choices=am.GetChoices();
            if(answer>=choices.Count)throw new Exception("Amelie route no longer has expected menu");
            selected.Add((string)choices[answer].GetType().GetField("_link",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(choices[answer]));
            am.ChooseAnswer(answer);lines.AddRange(Drain(am));
        }
        int count=am.GetChoices().Count;
        if(count>0) {
            if(route.Count>15)throw new Exception("Amelie menu loop");
            int total=0;
            for(int i=0;i<count;i++){var next=new List<int>(route);next.Add(i);total+=AmelieRoutes(row,mode,next);}
            return total;
        }
        string text=string.Join("\n",lines.ToArray());
        bool known=route[0]<2 || (route[0]==2 && route[1]==1);
        bool lied=route[0]==2 && route[1]==2;
        bool personal=selected.Contains("amelie-01-inquiry-truth");
        bool heart=HasText(text,"шло от сердца");
        bool company=HasText(text,"отчисления за показы");
        bool coherent=am.FindVariable("_Driver.Met.Amelie").value==1 && am.FindVariable("knowher").value==(known?1:0) && am.FindVariable("lie").value==(lied?1:0);
        coherent &= HasText(text,"В больнице вы смотрели японский фильм")== (route[0]==2);
        coherent &= HasText(text,"Это мне надо извиниться")== (route[0]==2 && route[1]!=1);
        coherent &= HasText(text,"ваша „подруга“")== (heart && lied);
        coherent &= HasText(text,"вашу бестактность это не оправдывает")== (heart && known && !lied);
        coherent &= HasText(text,"тень удовлетворения")== (personal && known);
        coherent &= HasText(text,"Наверное, поэтому моё лицо")==known;
        if(selected.Contains("amelie-01-iampornactress-notjust"))coherent &= company && !HasText(text,"в школе моего сына") && !heart;
        if(selected.Contains("amelie-01-iampornactress-surprise"))coherent &= !company && !heart && HasText(text,"Вы качаете головой");
        if(selected.Contains("amelie-01-shame"))coherent &= HasText(text,"начинают нервничать") && HasText(text,"двадцати пяти до тридцати пяти") && HasText(text,"его это возбуждает");
        if(selected.Contains("amelie-01-anger"))coherent &= HasText(text,"Можете себе представить? Любопытно") && HasText(text,"прожектор") && HasText(text,"через три минуты");
        coherent &= HasText(text,"спасателями с Тайваня")== selected.Contains("amelie-01-pirates");
        coherent &= HasText(text,"Там нет дурацких сценариев")== selected.Contains("amelie-01-inquiry-paper-why");
        coherent &= HasText(text,"пока я войду в дом") && HasText(text,"Она расплачивается") && HasText(text,"уезжаете");
        coherent &= HasText(text,"иногда она присматривала за моим сыном")== (mode!=3);
        coherent &= HasText(text,"Нож, движущийся вверх вдоль вашего бока")== (mode!=3);
        var clues=emitted.FindAll(a=>a.Contains("clue:"));coherent &= clues.Count==(mode==3?0:1);
        if(mode<3)coherent &= clues[0].Contains(mode==0?"clue_judge_customer_rumor_":mode==1?"clue_sand_customer_rumor_":"clue_angel_customer_rumor_");
        coherent &= am.FindVariable("_Found.Clue.Today").value==7;
        coherent &= emitted.Exists(a=>a.Contains("047_05"))==(mode!=3);
        var tips=emitted.FindAll(a=>a.Contains("tip"));coherent &= tips.Count==1 && tips[0].Contains("medium");
        coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
        coherent &= !am.can_continue && am.GetChoices().Count==0;
        amelieObservedModes[mode]++;
        Check(coherent,"Amelie complete route preserves woman/son, restored choices, lie/recognition callbacks and case/freeroam ending "+mode+"/"+string.Join(",",route));
        return 1;
    }

    static PrompterStory LeonieStart(Dialog d,string passage,int mode,int? boring) {
        var flags=new List<DialogVariable>();string[] names={"Judge","Sandman","Angel"};
        for(int i=0;i<3;i++)flags.Add(new DialogVariable("_Killer.Is."+names[i],mode==i?1:0));
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};story.StartDialog(flags,passage);
        if(boring.HasValue)Set(story.FindVariable("boring"),"_value",boring.Value);
        return story;
    }
    static int LeonieRoutes(Dictionary<string,object> row,int mode,List<int> route) {
        var le=LeonieStart(Load(row),"leonie-01-start",mode,null);
        var emitted=new List<string>();le.OnActionTriggered+=(evt)=>emitted.Add(evt.action);
        var lines=Drain(le);var selected=new List<string>();
        foreach(int answer in route) {
            var choices=le.GetChoices();if(answer>=choices.Count)throw new Exception("Leonie menu changed");
            selected.Add((string)choices[answer].GetType().GetField("_link",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(choices[answer]));
            le.ChooseAnswer(answer);lines.AddRange(Drain(le));
        }
        int count=le.GetChoices().Count;
        if(count>0) {
            if(route.Count>15)throw new Exception("Leonie menu loop");
            int total=0;for(int i=0;i<count;i++){var next=new List<int>(route);next.Add(i);total+=LeonieRoutes(row,mode,next);}return total;
        }
        string text=string.Join("\n",lines.ToArray());int expectedCare=0,expectedBoring=0;
        if(selected.Contains("leonie-01-silence2-something") || selected.Contains("leonie-01-silence2-stopworking"))expectedCare--;
        bool thesis=HasText(text,"аббатства Рансе");
        if(thesis) {
            if(selected.Contains("leonie-01-thesis-understand"))expectedBoring+=2;
            if(selected.Contains("leonie-01-thesis-surprise"))expectedBoring++;
            expectedBoring++;
            if(selected.Contains("leonie-01-thesis3-interesting"))expectedCare++;
            if(selected.Contains("leonie-01-thesis4-people")){expectedCare++;expectedBoring++;}
            if(selected.Contains("leonie-01-thesis4-silence"))expectedCare-=2;
            if(selected.Contains("leonie-01-thesis4-somethingelse"))expectedCare+=2;
            if(!selected.Contains("leonie-01-job-outro3"))expectedCare++;
        }
        bool changed=selected.Contains("leonie-01-job-outro3");
        bool classical=selected.Contains("leonie-01-question-music-classical");
        bool jazz=selected.Contains("leonie-01-question-music-jazz");
        bool rock=selected.Contains("leonie-01-question-music-rock");
        bool africa=selected.Contains("leonie-01-question-music-africa");
        var jazzVariable=le.FindVariable("music-jazz");bool hasJazzFlag=jazzVariable!=null;
        if(changed){if(classical || (jazz && hasJazzFlag))expectedCare-=2;if(rock || africa)expectedCare++;}
        bool coherent=le.FindVariable("care").value==expectedCare && le.FindVariable("boring").value==expectedBoring;
        coherent &= !HasText(text,"Вы не можете сдержать зевок") && !HasText(text,"Вы зеваете во второй раз") && !HasText(text,"подавляете огромный зевок");
        coherent &= HasText(text,"Парень бросил меня три месяца назад")==selected.Contains("leonie-01-thesis4-people");
        coherent &= HasText(text,"Они, конечно, ничего не говорят")== (thesis && !changed);
        coherent &= HasText(text,"Sister Pearl")== (changed && africa);
        coherent &= HasText(text,"Никогда не выносила Малера")== (changed && classical);
        coherent &= HasText(text,"Кажется, я ненавижу джаз")== (changed && jazz && hasJazzFlag);
        coherent &= HasText(text,"Мне от неё лучше")== (changed && rock);
        coherent &= HasText(text,"А потом мне доверились, меня обучили")==selected.Contains("leonie-01-job-outro2");
        coherent &= HasText(text,"монахинях XVIII века")== (thesis && !changed);
        coherent &= HasText(text,"Мне нужно подумать, но… спасибо за ваши слова")== (expectedCare>3);
        coherent &= HasText(text,"Не вышло… Но я правда ценю")== (expectedCare>1 && expectedCare<=3);
        coherent &= HasText(text,"кажется, внезапно понимает, где находится")== (expectedCare<=1);
        coherent &= HasText(text,"Polaroid, Roman Photo")==rock;
        coherent &= HasText(text,"мелодия коры, повторяющаяся без конца")==africa;
        var tips=emitted.FindAll(a=>a.Contains("tip"));coherent &= tips.Count==(expectedCare>3?2:1) && tips[0].Contains("medium");
        if(expectedCare>3)coherent &= tips[1].Contains("high");
        coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
        coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==0 && !le.can_continue && HasText(text,"потом трогаетесь");
        Check(coherent,"Leonie complete route preserves thesis/boyfriend, care thresholds, source music flags and exact endings "+mode+"/"+string.Join(",",route)+" jazz-variable="+hasJazzFlag+" care="+le.FindVariable("care").value+"/"+expectedCare+" boring="+le.FindVariable("boring").value+"/"+expectedBoring);
        return 1;
    }

    static void LeonieChecks(Dictionary<string,object> rows) {
        var row=(Dictionary<string,object>)rows["051_leonie_01"];var coverage=ReferenceFile("localization/leonie_fr_coverage.json");int total=0;
        foreach(Dictionary<string,object> profile in (IEnumerable)coverage["profiles"]) {
            int mode=Convert.ToInt32(profile["mode"]);int actual=LeonieRoutes(row,mode,new List<int>());total+=actual;
            Check(actual==Convert.ToInt32(profile["histories"]),"Leonie original full recursive traversal equals independent immutable source histories for profile "+mode+": "+actual);
        }
        Check(total==22544&&total==Convert.ToInt32(coverage["histories"]),"Leonie all22544complete finite original answer histories in three cases and no case; no route removed");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2375.json");
        Check((string)card["fullname"]=="Léonie Brémieux"&&Convert.ToInt32(card["age"])==28&&Convert.ToInt32(card["gender"])==0,"Leonie original female28card; PhD work not completed Russian doctorate, no inferred family names");
    }

    static PrompterStory ChildericStart(Dialog d,string passage,int hugo) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Hugo",hugo),new DialogVariable("_Driver.Met.Childeric",0)},passage);return story;
    }
    static int ChildericRoutes(Dictionary<string,object> row,int hugo,List<int> route) {
        var ch=ChildericStart(Load(row),"childeric-01-start",hugo);var emitted=new List<string>();
        ch.OnActionTriggered+=(evt)=>emitted.Add(evt.action);var lines=Drain(ch);var selected=new List<string>();
        foreach(int answer in route) {
            var choices=ch.GetChoices();if(answer>=choices.Count)throw new Exception("Childeric menu changed");
            selected.Add((string)choices[answer].GetType().GetField("_link",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(choices[answer]));
            ch.ChooseAnswer(answer);lines.AddRange(Drain(ch));
        }
        int count=ch.GetChoices().Count;
        if(count>0){if(route.Count>12)throw new Exception("Childeric story loop");int total=0;for(int i=0;i<count;i++){var next=new List<int>(route);next.Add(i);total+=ChildericRoutes(row,hugo,next);}return total;}
        string text=string.Join("\n",lines.ToArray());bool horse=selected.Contains("childeric-01-drive1-yourhorse");
        bool remembered=hugo==1 && horse;bool mount=selected.Contains("childeric-01-start-mount"),dragon=selected.Contains("childeric-01-start-dragon");
        int play=(selected.Contains("childeric-01-start-mylord")?1:0)+(horse?1:0)+(remembered?1:0);
        if(selected.Contains("childeric-01-start-where-paris") || selected.Contains("childeric-01-start-where-notredame"))play--;
        bool coherent=ch.FindVariable("_Driver.Met.Childeric").value==1 && ch.FindVariable("play").value==play;
        coherent &= ch.FindVariable("mount").value==(mount?1:0) && ch.FindVariable("dragon").value==(dragon?1:0) && ch.FindVariable("hub-story").value==((mount?1:0)+(dragon?1:0));
        coherent &= HasText(text,"ЭПИЧЕСКАЯ битва")== (route[0]==0);
        coherent &= HasText(text,"Вершина мира")==mount && HasText(text,"Сосок дьявола")==mount;
        coherent &= HasText(text,"золотая шкура")==dragon;
        coherent &= HasText(text,"К площади перед собором")==selected.Contains("childeric-01-start-where-paris");
        coherent &= HasText(text,"Это косплей")==remembered && HasText(text,"Чтобы он меня заметил")==remembered;
        coherent &= HasText(text,"Он мне… нравится")==remembered && HasText(text,"есть… другая")==remembered;
        coherent &= HasText(text,"Геральт")==horse;
        coherent &= HasText(text,"победитель гравий из Багровой шахты")==horse;
        coherent &= HasText(text,"кроличьими ушами")== (hugo==1);
        coherent &= emitted.Exists(a=>a.Contains("052_02"))==remembered;
        coherent &= emitted.Exists(a=>a.Contains("052_01"))== (hugo==1 && play>2);
        coherent &= HasText(text,"Мне надо добраться") && HasText(text,"на три дня… на три луны") && HasText(text,"лес Брерюн") && HasText(text,"Безрассудный");
        coherent &= HasText(text,"прищемляет палец") && HasText(text,"деньги за поездку");
        var tips=emitted.FindAll(a=>a.Contains("tip"));coherent &= tips.Count==1 && tips[0].Contains("low");
        coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
        coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==0 && !ch.can_continue;
        childericObservedProfiles[hugo]++;
        Check(coherent,"Childeric complete route preserves armor/world, independent Hugo callbacks and play>2 reveal "+hugo+"/"+string.Join(",",route));return 1;
    }

    static void CamilleRoutes(Dictionary<string,object> row,Dictionary<string,object> coverage) {
        var blocks=new Dictionary<string,Dictionary<string,object>>();
        foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        var reached=new HashSet<string>();var answersCovered=new HashSet<string>();int routeNumber=0;
        foreach(Dictionary<string,object> route in (IEnumerable)coverage["routes"]) {
            var story=new PrompterStory(Load(row),false);story.OnActionTriggered+=(a)=>{};
            story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Camille",0)},"camille-01-start");
            var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);var actual=Drain(story);
            foreach(object answer in (IEnumerable)route["answers"]) {story.ChooseAnswer(Convert.ToInt32(answer));actual.AddRange(Drain(story));}
            var expected=new List<string>();var passages=new HashSet<string>();
            foreach(string title in Strings(route["passages"])) {
                reached.Add(title);passages.Add(title);
                foreach(string line in Strings(blocks[title]["_lines"]))if(!line.StartsWith("$$"))expected.Add(line);
            }
            foreach(object edge in (IEnumerable)route["edges"]) {
                var values=new List<object>();foreach(object v in (IEnumerable)edge)values.Add(v);
                answersCovered.Add((string)values[0]+"/"+Convert.ToInt32(values[1]));
            }
            bool coherent=actual.Count==expected.Count;
            if(coherent)for(int i=0;i<actual.Count;i++)coherent &= actual[i]==expected[i];
            string text=string.Join("\n",actual.ToArray());bool expelled=passages.Contains("camille-01-getout-now"),cold=passages.Contains("camille-01-end-cold"),paid=passages.Contains("camille-01-end-regular2");
            coherent &= story.FindVariable("_Driver.Met.Camille").value==1 && story.FindVariable("getout").value==(expelled?1:0);
            coherent &= expelled || cold || paid;
            coherent &= HasText(text,"психоанализа-а-а")==passages.Contains("camille-01-question5");
            coherent &= !HasText(text,"психотерапии") && !HasText(text,"психотера-а-апи-и-и");
            coherent &= HasText(text,"Будем считать, что это ДА")==passages.Contains("camille-01-question5-outro");
            coherent &= HasText(text,"сто семнадцать секунд")==passages.Contains("camille-01-idea-bad");
            coherent &= HasText(text,"эти почти две минуты")==passages.Contains("camille-01-idea-bad");
            coherent &= HasText(text,"утром… в обед… и вечером")==passages.Contains("camille-01-idea2");
            coherent &= HasText(text,"Дом, который движется")==passages.Contains("camille-01-idea-bad-silence");
            coherent &= HasText(text,"подрывных инноваций")==passages.Contains("camille-01-idea-good-name");
            coherent &= HasText(text,"накопле-е-ения")==passages.Contains("camille-01-idea-good-silence");
            coherent &= HasText(text,"Нет, я — ваше будущее")==passages.Contains("camille-01-getout2");
            coherent &= HasText(text,"бизнес-школе")==expelled;
            coherent &= HasText(text,"Не совсем этого я жду от водителя такси-и-и")==cold;
            coherent &= HasText(text,"Сэкономите сто-о-олько")==paid;
            coherent &= HasText(text,"красивый сиреневый")==paid && !HasText(text,"лавандовый");
            coherent &= HasText(text,"присутствие на этой планете")!=cold;
            coherent &= emitted.Exists(a=>a.Contains("053_02"))==expelled && emitted.Exists(a=>a.Contains("053_03"))==paid;
            coherent &= emitted.Exists(a=>a.Contains("053_05"))==passages.Contains("camille-01-getout2-gross");
            coherent &= emitted.FindAll(a=>a.Contains("fare")).Count==(expelled?1:0);
            if(expelled)coherent &= emitted.Exists(a=>a.Contains("fare") && a.Contains("0"));
            var tips=emitted.FindAll(a=>a.Contains("tip"));coherent &= tips.Count==1 && tips[0].Contains("low");
            coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==(expelled?2:1);
            coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==0 && !story.can_continue && story.GetChoices().Count==0;
            Check(coherent,"Camille full representative route preserves exact spoken sequence, voice, manipulated answer and paid/free endings "+(++routeNumber));
        }
        Check(reached.Count==blocks.Count && reached.Count==Convert.ToInt32(coverage["passages_covered"]),"Camille routes cover every original passage: "+reached.Count);
        int expectedChoices=0;foreach(var p in blocks.Values)foreach(object c in (IEnumerable)p["_choices"])expectedChoices++;
        Check(answersCovered.Count==expectedChoices && expectedChoices==Convert.ToInt32(coverage["answers_covered"]),"Camille routes cover every original answer edge: "+expectedChoices+"; merged equivalent continuations, not all Cartesian combinations");
        var paymentChoices=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks["camille-01-end-regular"]["_choices"])paymentChoices.Add(c);
        Check((string)paymentChoices[2]["_text"]=="(Кивнуть.)","Camille original French nod agrees with not accepting cryptocurrency; English shake is not forced into Russian");
    }

    static PrompterStory LudivineStart(Dialog d,string passage,int mode,int tired,int smoking,int knows,int? singular) {
        var flags=new List<DialogVariable>{new DialogVariable("_Driver.Is.Exhausted",tired),new DialogVariable("_Driver.Is.Smoking",smoking),new DialogVariable("_Ludivine.Knows.Survivor",knows)};
        string[] names={"Judge","Sandman","Angel"};for(int i=0;i<3;i++)flags.Add(new DialogVariable("_Killer.Is."+names[i],mode==i?1:0));
        if(singular.HasValue)flags.Add(new DialogVariable("interruption",singular.Value));
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};story.StartDialog(flags,passage);return story;
    }
    static void LudivineHello(PrompterStory story,List<string> text,int hello,int response,int stare) {
        story.ChooseAnswer(hello);text.AddRange(Drain(story));
        if(hello<3){story.ChooseAnswer(response);text.AddRange(Drain(story));}
        if(hello==3 || response==3){story.ChooseAnswer(stare);text.AddRange(Drain(story));}
    }
    static void LudivineHistory(PrompterStory story,List<string> text,int mask,int job) {
        story.ChooseAnswer((mask&1)!=0?0:1);text.AddRange(Drain(story));
        story.ChooseAnswer((mask&2)!=0?0:1);text.AddRange(Drain(story));
        story.ChooseAnswer(job);text.AddRange(Drain(story));
        story.ChooseAnswer((mask&4)!=0?0:1);text.AddRange(Drain(story));
        story.ChooseAnswer((mask&8)!=0?0:1);text.AddRange(Drain(story));
    }
    static int ValueOrZero(PrompterStory story,string name) {var variable=story.FindVariable(name);return variable==null?0:variable.value;}
    static bool LudivineFinal(PrompterStory story,List<string> text,List<string> emitted,int knows,bool threat,bool outside) {
        string all=string.Join("\n",text.ToArray());var tips=emitted.FindAll(a=>a.Contains("tip"));
        bool ok=!story.can_continue && story.GetChoices().Count==0 && tips.Count==1 && tips[0].Contains("none");
        ok &= emitted.FindAll(a=>a.Contains("travel") || a.Contains("clue:")).Count==0;
        ok &= emitted.FindAll(a=>a.Contains("fare")).Count==(outside?1:0);
        if(outside)ok &= emitted.Exists(a=>a.Contains("fare") && a.Contains("0"));
        ok &= ValueOrZero(story,"_Ludivine.Knows.Survivor")==knows && !emitted.Exists(a=>a.Contains("054_03"));
        ok &= HasText(all,"Вы везли пассажира") && HasText(all,"литературный редактор") && HasText(all,"уйти из своей редакции") && HasText(all,"Двадцать два сорок шесть");
        ok &= HasText(all,"а шин нет")==threat && HasText(all,"Бегун, одетый")==outside;
        ok &= HasText(all,"Ничего нового") && HasText(all,"Вы резко трогаетесь с места") && HasText(all,"шорох за спиной") && HasText(all,"Мимолётный блик");
        // FR-only reciprocal thanks is retained in the final carrier slot; not a second driver line.
        ok &= HasText(all,"Это вам спасибо")== (knows==1 && !threat);
        return ok;
    }
    static void LudivineRoutes(Dictionary<string,object> row) {
        int openingCount=0,interruptCount=0,apologyCount=0;
        for(int hello=0;hello<4;hello++)for(int response=0;response<4;response++)for(int stare=0;stare<2;stare++) {
            if(hello==3 && response!=0)continue;if(hello<3 && response!=3 && stare!=0)continue;
            for(int mode=0;mode<4;mode++)for(int condition=0;condition<4;condition++) {
                var story=LudivineStart(Load(row),"ludivine-01-start",mode,condition>>1,condition&1,0,null);
                var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);var text=Drain(story);
                LudivineHello(story,text,hello,response,stare);bool looked=hello==3 || response==3,continued=looked && stare==1;
                string before=string.Join("\n",text.ToArray());bool ok=ValueOrZero(story,"ludivine-approved")== (looked?(continued?2:1):0);
                ok &= HasText(before,"бесконечная усталость последних дней")== (continued && condition>=2);
                ok &= HasText(before,"давно не курили")== (continued && condition==0);
                ok &= HasText(before,"мысли путаются от расследования")== (continued && condition==1);
                ok &= HasText(before,"женщина… Судья")== (mode==0) && HasText(before,"пассажирка… Песочный человек")== (mode==1) && HasText(before,"женщина… Ангел смерти")== (mode==2);
                LudivineHistory(story,text,0,3);story.ChooseAnswer(1);text.AddRange(Drain(story));
                ok &= HasText(string.Join("\n",text.ToArray()),"Журналистка вас разыскивает");
                story.ChooseAnswer(1);text.AddRange(Drain(story));
                ok &= HasText(string.Join("\n",text.ToArray()),"А дома уничтожу всё") && HasText(string.Join("\n",text.ToArray()),"Нет, не нужно");
                ok &= LudivineFinal(story,text,emitted,1,false,false);
                Check(ok,"Ludivine complete introduction/case/state route preserves tired-before-smoking priority, profession and survivor knowledge "+hello+"/"+response+"/"+stare+"/"+mode+"/"+condition);openingCount++;
            }
        }
        for(int mask=0;mask<16;mask++)for(int job=0;job<4;job++) {
            var story=LudivineStart(Load(row),"ludivine-01-start",0,0,1,0,null);var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);var text=Drain(story);
            LudivineHello(story,text,0,0,0);LudivineHistory(story,text,mask,job);
            int interruptions=((mask&1)!=0?1:0)+((mask&2)!=0?1:0)+((mask&4)!=0?1:0)+((mask&8)!=0?1:0)+(job<2?1:0);
            int earlier=((mask&1)!=0?1:0)+((mask&2)!=0?1:0)+(job<2?1:0)+((mask&4)!=0?1:0);
            string before=string.Join("\n",text.ToArray());bool ok=ValueOrZero(story,"interruptions")==interruptions;
            ok &= HasText(before,"ЗАТКНИТЕСЬ! ДАЙТЕ МНЕ СКАЗАТЬ")== ((mask&4)!=0 && earlier>3);
            ok &= !HasText(before,"КАЖДЫЕ ДВЕ МИНУТЫ");
            ok &= HasText(before,"Хотя извиняться как следует")== (interruptions>4);
            ok &= HasText(before,"Да, мадам")== ((mask&1)!=0) && !HasText(before,"мадемуазель");
            ok &= ValueOrZero(story,"ludivine-approved")== (job==0?-2:job==1?-3:job==2?1:0);
            story.ChooseAnswer(0);Drain(story);story.ChooseAnswer(0);text.AddRange(Drain(story));story.ChooseAnswer(0);text.AddRange(Drain(story));
            story.ChooseAnswer(0);text.AddRange(Drain(story));story.ChooseAnswer(0);text.AddRange(Drain(story));
            ok &= HasText(string.Join("\n",text.ToArray()),"Сдержанно, в классическом духе") && HasText(string.Join("\n",text.ToArray()),"Справляться со всем этим гневом");
            ok &= LudivineFinal(story,text,emitted,0,false,false);
            Check(ok,"Ludivine full interruption route preserves strict thresholds, unreachable >5 and distinct singular end flag "+mask+"/"+job);interruptCount++;
        }
        for(int first=0;first<4;first++)for(int final=0;final<3;final++)for(int repeats=0;repeats<4;repeats++)for(int disclaimer=0;disclaimer<2;disclaimer++)for(int answer=0;answer<3;answer++) {
            var story=LudivineStart(Load(row),"ludivine-01-start",0,0,1,0,null);var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);var text=Drain(story);
            LudivineHello(story,text,0,0,0);LudivineHistory(story,text,0,3);story.ChooseAnswer(0);Drain(story);story.ChooseAnswer(first);text.AddRange(Drain(story));
            for(int n=0;n<repeats;n++){story.ChooseAnswer(1);text.AddRange(Drain(story));}
            story.ChooseAnswer(final==0?0:final==1?2:3);text.AddRange(Drain(story));
            story.ChooseAnswer(disclaimer);text.AddRange(Drain(story));
            int classic=(first==0?1:0)+(final==0?2:0),real=(first==1?1:0)+(final==1?2:0),tooMuch=(first==2?1:0)+(final==2?2:0),quick=(first==3?1:0)+repeats;
            bool ok=ValueOrZero(story,"excuse-classic")==classic && ValueOrZero(story,"excuse-real")==real && ValueOrZero(story,"excuse-toomuch")==tooMuch && ValueOrZero(story,"excuse-quick")==quick;
            string before=string.Join("\n",text.ToArray());
            ok &= HasText(before,"Это мои проблемы")== (disclaimer==0);
            ok &= HasText(before,"Сдержанно, в классическом духе")== (classic>2);
            ok &= HasText(before,"говорили от души")== (classic<=2 && real>2);
            ok &= HasText(before,"С любезностями")== (classic<=2 && real<=2 && tooMuch>2);
            ok &= HasText(before,"Коротко. Но действенно")== (classic<=2 && real<=2 && tooMuch<=2 && quick>2);
            ok &= HasText(before,"Не знаю, что сказать")== (classic<=2 && real<=2 && tooMuch<=2 && quick<=2);
            story.ChooseAnswer(answer);text.AddRange(Drain(story));ok &= LudivineFinal(story,text,emitted,0,false,false);
            Check(ok,"Ludivine full apology route preserves weighted forms, 0-3 quick loop repetitions, priority and absent memory "+first+"/"+final+"/"+repeats+"/"+disclaimer+"/"+answer);apologyCount++;
        }
        for(int knows=0;knows<2;knows++)for(int after=0;after<2;after++) {
            var story=LudivineStart(Load(row),"ludivine-01-start",2,0,1,knows,null);var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);var text=Drain(story);
            LudivineHello(story,text,0,0,0);LudivineHistory(story,text,0,3);story.ChooseAnswer(after==0?0:1);Drain(story);
            if(after==0){story.ChooseAnswer(0);text.AddRange(Drain(story));story.ChooseAnswer(0);text.AddRange(Drain(story));story.ChooseAnswer(1);text.AddRange(Drain(story));story.ChooseAnswer(1);text.AddRange(Drain(story));}
            else{ text.AddRange(Drain(story));story.ChooseAnswer(0);text.AddRange(Drain(story));}
            bool ok=LudivineFinal(story,text,emitted,after==1?1:knows,false,false);
            ok &= HasText(string.Join("\n",text.ToArray()),"А дома уничтожу всё")== (after==1 || knows==1);
            Check(ok,"Ludivine persisted survivor knowledge selects destruction promise independently of current apology/attack route "+knows+"/"+after);
        }
        for(int reply=0;reply<4;reply++)for(int outside=0;outside<3;outside++) {
            var story=LudivineStart(Load(row),"ludivine-01-start",3,0,1,0,null);var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);var text=Drain(story);
            LudivineHello(story,text,0,0,0);LudivineHistory(story,text,0,3);story.ChooseAnswer(2);text.AddRange(Drain(story));story.ChooseAnswer(reply);text.AddRange(Drain(story));story.ChooseAnswer(outside);text.AddRange(Drain(story));
            Check(LudivineFinal(story,text,emitted,0,true,outside<2),"Ludivine full threat route preserves no disclosure, three outside replies, jogger and no ride "+reply+"/"+outside);
        }
        Check(openingCount==272 && interruptCount==64 && apologyCount==288,"Ludivine bounded full route coverage: 272 introductions/states, 64 interruption patterns, 288 apology variants; not an infinite-loop enumeration");
        Check(Strings(row["_user_datas"])[0]=="passengercall: NO PASSENGER CALL","Ludivine technical no-call marker preserved literally");
        bool declaredApproval=false;foreach(Dictionary<string,object> routeVar in (IEnumerable)row["_variables"])declaredApproval |= (string)routeVar["_name"]=="ludivine-approved";
        Check(!declaredApproval,"Ludivine original local-variable list does not declare dynamically created approval");
        var controlled=Load(row);foreach(DialogVariable v in (IEnumerable)controlled.GetType().GetField("_variables",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(controlled))if(v.name=="interruptions")Set(v,"_value",5);
        var direct=LudivineStart(controlled,"ludivine-01-story6-interruption",0,0,1,0,null);string directText=string.Join("\n",Drain(direct).ToArray());
        Check(directText.Contains("КАЖДЫЕ ДВЕ МИНУТЫ") && ValueOrZero(direct,"interruptions")==6,"Ludivine >5 interruption block works only with controlled direct initial counter5");
        direct=LudivineStart(Load(row),"ludivine-01-excuses02-quick",0,0,1,0,null);Drain(direct);
        Check(ValueOrZero(direct,"excuse-quick")==2 && direct.GetChoices().Count==2,"Ludivine isolated second quick block preserved; controlled direct start, no invented incoming link");
        direct=LudivineStart(Load(row),"ludivine-01-end2",0,0,1,0,4);var directActions=new List<string>();direct.OnActionTriggered+=(a)=>directActions.Add(a.action);Drain(direct);
        Check(directActions.Exists(a=>a.Contains("054_03")),"Ludivine final reveal checks separately supplied singular interruption4, not normal plural counter");
    }

    static PrompterStory JulianStart(Dialog d,int alreadyHelped) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Julian",0),new DialogVariable("_Julian.Help",alreadyHelped)},"julian-01-start");return story;
    }
    static void JulianRoutes(Dictionary<string,object> row,Dictionary<string,object> peter) {
        int count=0,actualMet=0;
        for(int inherited=0;inherited<2;inherited++)for(int greeting=0;greeting<4;greeting++)for(int question=0;question<3;question++)for(int comfort=0;comfort<2;comfort++)for(int eye=0;eye<4;eye++) {
            var story=JulianStart(Load(row),inherited);var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);
            var text=Drain(story);string start=string.Join("\n",text.ToArray());bool ok=HasText(start,"Жюльян Паз") && HasText(start,"в ближайшие дни вы услышите обо мне") && !HasText(start,"архивист");
            story.ChooseAnswer(greeting);text.AddRange(Drain(story));story.ChooseAnswer(question);text.AddRange(Drain(story));
            string all=string.Join("\n",text.ToArray());
            ok &= HasText(all,"Почему…")== (question==0);
            ok &= HasText(all,"ЖЮЛЬЯН: «Как?»")== (question==2);
            ok &= HasText(all,"Выбор есть всегда")== (question==1);
            ok &= HasText(all,"Два года назад") && HasText(all,"неправительственными организациями") && HasText(all,"двадцать пять лет махинаций") && HasText(all,"Блядь… ёбаный в рот");
            story.ChooseAnswer(comfort);text.AddRange(Drain(story));
            ok &= HasText(string.Join("\n",text.ToArray()),"побыть одному")== (comfort==1);
            story.ChooseAnswer(eye==0?1:0);text.AddRange(Drain(story));
            if(eye!=0){story.ChooseAnswer(eye==1?1:0);text.AddRange(Drain(story));}
            if(eye>=2){story.ChooseAnswer(eye==2?0:1);text.AddRange(Drain(story));}
            all=string.Join("\n",text.ToArray());
            bool help=eye>=2;
            ok &= HasText(all,"наверное, не больше тридцати пяти") && HasText(all,"у корней его волос… седину")== (eye!=0);
            ok &= HasText(all,"Пытаетесь запомнить моё лицо")== (eye!=0);
            ok &= HasText(all,"Я расследую дело Судьи")==help;
            ok &= HasText(all,"Его жертва в ноябре")==help;
            ok &= HasText(all,"Я отказываюсь им помогать")== (eye==2);
            ok &= HasText(all,"Быть таким храбрым")== (eye==3);
            ok &= HasText(all,"Запах пороха")==help;
            ok &= HasText(all,"Ни слова полиции")==help && HasText(all,"Вы называете имена подозреваемых")==help;
            ok &= HasText(all,"ничего не обещаю")==help && HasText(all,"Через диспетчерскую")==help;
            ok &= story.FindVariable("_Julian.Help").value==(help?1:inherited);
            actualMet=story.FindVariable("_Driver.Met.Julian").value;ok &= actualMet==1;
            ok &= emitted.Exists(a=>a.Contains("055_01"))==help && emitted.Exists(a=>a.Contains("055_02"))== (eye==2) && emitted.Exists(a=>a.Contains("055_03"))== (eye==3);
            ok &= HasText(all,"кафе") && HasText(all,"Этот замок такой… большой. Такой пустой") && HasText(all,"Сдачи не надо") && HasText(all,"Сам боюсь забыть") && HasText(all,"берёт лежащую рядом газету");
            var tips=emitted.FindAll(a=>a.Contains("tip"));ok &= tips.Count==1 && tips[0].Contains("medium");
            ok &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
            ok &= emitted.FindAll(a=>a.Contains("fare") || a.Contains("clue:")).Count==0 && !story.can_continue && story.GetChoices().Count==0;
            Check(ok,"Julian complete route preserves male voice, age estimate, public leak, secrecy and distinct Help/Met flags "+inherited+"/"+greeting+"/"+question+"/"+comfort+"/"+eye);count++;
        }
        Check(count==192,"Julian all full answer paths with fresh and inherited help: "+count);
        for(int otherMet=0;otherMet<4;otherMet++) {
            var radio=PeterFlags(Load(peter),"Peter-01-radio02",(otherMet&1) | (actualMet<<1) | ((otherMet&2)<<1),3);
            string text=string.Join("\n",Drain(radio).ToArray());
            Check(HasText(text,"Разоблачителя расспрашивают")== ((otherMet&1)==0),"Julian actual meeting flag opens Peter's whistleblower radio after Alicia priority and before Shohreh "+otherMet);
        }

        var reference=ReferenceFile("localization/julian_fr_coverage.json");int total=0;foreach(Dictionary<string,object> profile in (IEnumerable)reference["profiles"]){Check(Convert.ToInt32(profile["histories"])==96,"Julian independent source counts96histories with inheritedHelp="+profile["inherited_help"]);total+=Convert.ToInt32(profile["histories"]);}
        Check(count==total&&count==192&&Convert.ToInt32(reference["ordinary_passages"])==23&&Convert.ToInt32(reference["answers"])==15,"Julian original192joint profiles retained, equal independent immutable23blocks15answers; document920scheduler not executed here");
        var data=Strings(row["_user_datas"]);Check(data.Count==7&&data[0]=="passengercall: «Мне нужно прокатиться…»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==6,"Julian useful French need-a-drive call retained at index0 with6original animations despite sourcehas_passenger_call0; no new icons");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2379.json");Dictionary<string,object> encounter=null;foreach(Dictionary<string,object> item in (IEnumerable)card["encounters"]){encounter=item;break;}var content=(Dictionary<string,object>)encounter["dialog_content"];
        Check((string)card["fullname"]=="Julian Paz"&&Convert.ToInt32(card["age"])==36&&Convert.ToInt32(card["gender"])==3&&Convert.ToInt32(content["has_passenger_call"])==0&&Convert.ToInt32(encounter["appears_only_for_killer"])==1&&Convert.ToInt32(encounter["has_clues_for_killer"])==1,"Julian source male36card, estimate35separate, case1sourceflags/usefulrequest; actualUnityselector not proved");
    }

    static int KaderRoutes(Dictionary<string,object> row,List<int> route) {
        var story=Start(Load(row),"kader-01-start");var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);
        var text=Drain(story);var selected=new List<string>();
        foreach(int answer in route) {
            var choices=story.GetChoices();if(answer>=choices.Count)throw new Exception("Kader menu changed");
            selected.Add((string)choices[answer].GetType().GetField("_link",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(choices[answer]));
            story.ChooseAnswer(answer);text.AddRange(Drain(story));
        }
        int choiceCount=story.GetChoices().Count;
        if(choiceCount>0){if(route.Count>12)throw new Exception("Kader menu loop");int total=0;for(int i=0;i<choiceCount;i++){var next=new List<int>(route);next.Add(i);total+=KaderRoutes(row,next);}return total;}
        int trust=0;
        foreach(string title in selected) {
            if(title=="kader-01-cops-ok" || title=="kader-01-cops-always" || title=="kader-01-book-review" || title=="kader-01-woman-others" || title=="kader-01-woman-smile")trust++;
            if(title=="kader-01-woman-laugh")trust+=2;
        }
        bool quiet=selected.Contains("kader-01-silence2") || selected.Contains("kader-01-silence2-ok") || selected.Contains("kader-01-silence2-nothing");
        bool high=!quiet && trust>5,warm=!quiet && trust>2 && trust<=5;
        string all=string.Join("\n",text.ToArray());bool ok=story.FindVariable("trust").value==trust;
        ok &= HasText(all,"дурная шутка")==selected.Contains("kader-01-cops-ok");
        ok &= HasText(all,"книгу в мягкой обложке") ==selected.Contains("kader-01-cops-always");
        ok &= HasText(all,"белой…")==selected.Contains("kader-01-silence");
        ok &= HasText(all,"обращение на «ты»")==selected.Contains("kader-01-silence-reading");
        ok &= HasText(all,"Техническом лицее")==selected.Contains("kader-01-book-why");
        ok &= HasText(all,"Ты тоже читал?")==selected.Contains("kader-01-woman-others");
        ok &= HasText(all,"Или своим корешам")==selected.Contains("kader-01-woman-others");
        ok &= HasText(all,"исторические штуки")==selected.Contains("kader-01-woman-dumas");
        ok &= HasText(all,"муниципальной полиции")==high && HasText(all,"Ей тридцать семь")==high;
        ok &= HasText(all,"Бывают девчонки")==warm;
        ok &= HasText(all,"завтра рано на работу")==!quiet;
        ok &= HasText(all,"облупившейся краской")==quiet;
        ok &= HasText(all,"Пассажир протягивает вам деньги")==!quiet && HasText(all,"Пассажир сразу платит")==quiet;
        ok &= emitted.FindAll(a=>a.Contains("056_03")).Count==(selected.Contains("kader-01-cops-always")?1:0)+(high?1:0);
        var tips=emitted.FindAll(a=>a.Contains("tip"));ok &= tips.Count==1 && tips[0].Contains(quiet?"none":"low");
        ok &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1;
        ok &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count== (quiet || (!high && !warm)?1:0);
        ok &= emitted.FindAll(a=>a.Contains("fare") || a.Contains("clue:")).Count==0 && !story.can_continue && story.GetChoices().Count==0;
        Check(ok,"Kader complete route preserves paperback, adult ages, familiar speech, strict trust and original stop/reveal/tip behavior "+string.Join(",",route)+" trust="+trust);return 1;
    }

    static void KaderChecks(Dictionary<string,object> row) {
        int kaderCount=KaderRoutes(row,new List<int>());
        Check(kaderCount==138,"Kader all full answer paths: "+kaderCount);
        var reference=ReferenceFile("localization/kader_fr_validation_routes.json");var seen=new HashSet<string>();var edges=new HashSet<string>();int rides=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,edges);
            var path=Strings(r["passages"]);string all=string.Join("\n",text.ToArray());bool ok=!story.can_continue&&story.GetChoices().Count==0;
            if(path.Contains("kader-01-dumas-alt"))ok &= all.Contains("строит из себя привереду")&&!all.Contains("недотрогу");
            if(path.Contains("kader-01-cops-what"))ok &= all.Contains("Наверное, ему едва двадцать");
            if(path.Contains("kader-01-silence-reading"))ok &= all.Contains("Кажется, пассажира удивил")&&all.Contains("только собирался нашкодить")&&all.Contains("в ярости");
            if(path.Contains("kader-01-book-review"))ok &= all.Contains("поднимает голову")&&all.Contains("Ну, забавно… Это…");
            Check(ok,"Kader full immutable answer history "+(++rides)+" exact French-aligned text/menu/state/orderedcommands; no sexual claim/age rewrite/completed mischief");
        }
        Check(rides==138&&kaderCount==rides&&Convert.ToInt32(reference["full_answer_histories"])==138&&seen.Count==28&&edges.Count==19&&Convert.ToInt32(reference["ordinary_passages"])==28&&Convert.ToInt32(reference["answers"])==19,"Kader all138old and138exact finite histories cover28original ordinary blocks19answers; strict trust/repeated05603/stop asymmetry unchanged");
        bool menusOk=true;var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> block in (IEnumerable)row["_passages"])blocks[(string)block["_title"]]=block;
        foreach(Dictionary<string,object> menu in (IEnumerable)reference["menus"]) {
            var texts=Strings(menu["texts"]);var links=Strings(menu["links"]);int i=0;
            foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)menu["passage"]]["_choices"]){menusOk &= i<texts.Count&&texts[i]==(string)c["_text"]&&links[i]==(string)c["_link"];i++;}
            menusOk &= i==texts.Count;
        }
        Check(menusOk,"Kader all19original answers preserve exact French-aligned meaning/order/targets and existing ENcarrier icons, including silence-marker nothing-do answer");
        var data=Strings(row["_user_datas"]);Check(data.Count==5&&data[0]=="passengercall: «Не успел на последнее метро… Мне в Сент-Уэн»."&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==4,"Kader useful French last-metro call/original carrier prefix and4animations despite sourcehascall0; no new icons");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2380.json");Dictionary<string,object> encounter=null;foreach(Dictionary<string,object> item in (IEnumerable)card["encounters"]){encounter=item;break;}var content=(Dictionary<string,object>)encounter["dialog_content"];
        Check((string)card["fullname"]=="Kader Bergaoui"&&Convert.ToInt32(card["age"])==21&&Convert.ToInt32(card["gender"])==3&&Convert.ToInt32(content["has_passenger_call"])==0&&Convert.ToInt32(content["has_travel"])==0,"Kader source male21separate from narrator estimate/child comparison, woman37onlytrust6; original card hascall/travel0not removed commands");
    }

    static HashSet<string> AnnabelleCovered=new HashSet<string>();
    static PrompterStory AnnabelleStart(Dialog d,string passage,int met) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Shohreh",met),new DialogVariable("_Driver.Met.Annabelle",0)},passage);return story;
    }
    static int AnnabelleRoute(Dictionary<string,object> row,int met,List<int> route,Dictionary<string,object> reference) {
        var story=AnnabelleStart(Load(row),"annabelle-01-start",met);var actions=new List<string>();story.OnActionTriggered+=(a)=>actions.Add(a.action);
        var text=Drain(story);var selected=new List<string>();
        var edges=new List<object>();foreach(object edge in (IEnumerable)reference["edges"])edges.Add(edge);int edgeIndex=0;
        var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        foreach(int answer in route) {
            var choices=story.GetChoices();if(answer>=choices.Count)throw new Exception("Annabelle menu changed");
            var edge=new List<object>();foreach(object value in (IEnumerable)edges[edgeIndex++])edge.Add(value);
            var expectedMenu=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)edge[0]]["_choices"])expectedMenu.Add(c);
            if(choices.Count!=expectedMenu.Count || answer!=Convert.ToInt32(edge[1]))throw new Exception("Annabelle source menu count/edge changed");
            for(int i=0;i<choices.Count;i++)if((string)choices[i].GetType().GetField("_text",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(choices[i])!=(string)expectedMenu[i]["_text"])throw new Exception("Annabelle source menu text changed");
            selected.Add((string)choices[answer].GetType().GetField("_link",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(choices[answer]));
            story.ChooseAnswer(answer);text.AddRange(Drain(story));
        }
        int count=story.GetChoices().Count;
        if(count>0)throw new Exception("Annabelle representative route is incomplete");
        string all=string.Join("\n",text.ToArray());
        bool saved=selected.Contains("annabelle-01-phone-out"),yogaStop=selected.Contains("annabelle-01-yoga-stop"),frequency=selected.Contains("annabelle-01-radio-frequency");
        bool job=selected.Contains("annabelle-01-start-holidayB") || selected.Contains("annabelle-01-start-where") || selected.Contains("annabelle-01-start-holiday-sorry");
        int phone=selected.Contains("annabelle-01-phone03")?3:selected.Contains("annabelle-01-phone02")?2:1;
        int initialAngry=(selected.Contains("annabelle-01-start-holidayA")?1:0)+(selected.Contains("annabelle-01-start-holiday-where")?1:0);
        bool excuse=selected.Contains("annabelle-01-start-holiday-sorry");
        bool rock=selected.Contains("annabelle-01-radio-rock");
        bool asked=selected.Contains("annabelle-01-radio-rock-askchange") || selected.Contains("annabelle-01-radio-rock-change");
        bool spontaneous=rock && !asked && (initialAngry<2 || excuse || saved);
        bool rockStory=rock && (asked || spontaneous);
        bool ok=story.FindVariable("_Driver.Met.Annabelle").value==1 && story.FindVariable("job").value==(job?1:0);
        ok &= story.FindVariable("saved").value==(saved?1:0) && story.FindVariable("frequency").value==(frequency?1:0);
        ok &= story.FindVariable("angry").value==(yogaStop?0:initialAngry) && story.FindVariable("phone").value==(saved?phone:3);
        ok &= !HasText(all,"показали своё тайное место") && !HasText(all,"И вы берегите себя") && !HasText(all,"Из-за таких, как вы");
        ok &= HasText(all,"Связь сейчас прервётся")==saved && HasText(all,"не дают дышать")==saved;
        ok &= HasText(all,"проклятие под названием")== (saved && phone==3);
        ok &= HasText(all,"Папа как раз собирался передать")== (saved && phone==2);
        ok &= HasText(all,"Это могло тянуться часами")== (saved && phone==1);
        ok &= HasText(all,"Как после йоги, только")== (frequency && !saved);
        ok &= HasText(all,"всё сжимается внутри")== (!saved && !frequency);
        ok &= HasText(all,"Девушка спит")==yogaStop && HasText(all,"Несколько минут вы смотрите")==yogaStop;
        ok &= HasText(all,"Ты не виновата в том, что я занялась фотографией")== (phone==3) && HasText(all,"Сирии")==false;
        ok &= HasText(all,"Изменить мир")==rockStory;
        ok &= HasText(all,"Здесь совершенно безопасно")==false; // Answer text is presented by the menu, not replayed as narration.
        ok &= actions.FindAll(a=>a.Contains("057_03")).Count==(saved?1:0) && actions.FindAll(a=>a.Contains("057_04")).Count==(yogaStop?1:0);
        ok &= actions.FindAll(a=>a.Contains("057_05")).Count==(frequency?1:0) && actions.FindAll(a=>a.Contains("057_06")).Count==(spontaneous?1:0);
        var tips=actions.FindAll(a=>a.Contains("tip"));ok &= tips.Count==(saved?2:1) && tips[0].Contains("low") && (!saved || tips[1].Contains("high"));
        ok &= actions.FindAll(a=>a.Contains("destination")).Count==(yogaStop?2:0);
        ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==(yogaStop?2:1);
        ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==(yogaStop?2:1);
        ok &= HasText(all,"оплачивает поездку") && !story.can_continue && story.GetChoices().Count==0;
        var expectedText=Strings(reference["expected_texts"]);ok &= text.Count==expectedText.Count;
        if(text.Count==expectedText.Count)for(int i=0;i<text.Count;i++)ok &= text[i]==expectedText[i];
        AngeliqueActions(reference,actions);
        foreach(var pair in (Dictionary<string,object>)reference["expected_variables"])ok &= story.FindVariable(pair.Key)!=null && story.FindVariable(pair.Key).value==Convert.ToInt32(pair.Value);
        // Whole visible blocks, not isolated matching words, establish coverage in complete routes.
        foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"]) {
            var visible=new List<string>();foreach(string line in Strings(p["_lines"]))if(!line.TrimStart().StartsWith("$$"))visible.Add(line);
            string title=(string)p["_title"];
            if(visible.Count>0 && HasText(all,string.Join("\n",visible.ToArray())))AnnabelleCovered.Add(title);
        }
        if(!yogaStop)AnnabelleCovered.Add(met==1?"annabelle-01-radio-shohreh":"annabelle-01-radio-normal");
        Check(ok,"Annabelle complete route preserves parent stages, rescue context, radio priority, yoga trip and unchanged default thresholds "+met+"/"+string.Join(",",route));return 1;
    }

    static void AnnabelleSharedFrequency(Dictionary<string,object> shohreh,Dictionary<string,object> annabelle) {
        var first=ShohrehStart(Load(shohreh));Drain(first);int turns=0;
        while(first.GetChoices().Count>0) {if(++turns>20)throw new Exception("Shohreh shared frequency route loop");first.ChooseAnswer(0);Drain(first);}
        var second=AnnabelleStart(Load(annabelle),"annabelle-01-radio",first.FindVariable("_Driver.Met.Shohreh").value);Drain(second);
        bool ok=first.FindVariable("_Driver.Met.Shohreh").value==1 && second.GetChoices().Count==4;
        second.ChooseAnswer(3);string text=string.Join("\n",Drain(second).ToArray());
        ok &= second.FindVariable("frequency").value==1 && HasText(text,"музыкальная волна") && HasText(text,"Как после йоги, только") && !HasText(text,"89,4");
        Check(ok,"Shohreh actual completed meeting unlocks Annabelle's fourth radio choice and ambiguous frequency; numeric value not inserted");
    }
    static void AnnabelleChecks(Dictionary<string,object> row) {
        var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=50000000;
        var reference=serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText("localization/annabelle_validation_routes.json",Encoding.UTF8));int total=0;
        foreach(Dictionary<string,object> route in (IEnumerable)reference["routes"]) {
            var answers=new List<int>();foreach(object answer in (IEnumerable)route["answers"])answers.Add(Convert.ToInt32(answer));
            total+=AnnabelleRoute(row,Convert.ToInt32(route["met"]),answers,route);
        }
        var absent=new HashSet<string>{"annabelle-01-airport-yoga","annabelle-01-airport3-yoga","annabelle-01-airport-goodB-yoga","annabelle-01-airport3-angry"};
        foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])Check(AnnabelleCovered.Contains((string)p["_title"])!=absent.Contains((string)p["_title"]),"Annabelle full-route default passage coverage "+p["_title"]);
        Check(total==1156 && AnnabelleCovered.Count==83,"Annabelle all1156original representative full routes with/without prior Shohreh: "+total+"; 83 default-reachable passages, 54 answers and "+reference["menu_states"]+" variable menu states; four controlled blocks separately");
        foreach(string title in absent) {
            var story=AnnabelleStart(Load(row),title,1);string text=string.Join("\n",Drain(story).ToArray());
            Check(HasText(text,title.EndsWith("airport-yoga")?"натриевых фонарей":title.EndsWith("airport3-yoga")?"тайное место":title.EndsWith("goodB-yoga")?"И вы берегите себя":"Из-за таких, как вы"),"Annabelle translated source block controlled direct start only "+title);
        }
        // Original conditions accept externally supplied values; this is not default reachability evidence.
        for(int yoga=0;yoga<2;yoga++)for(int saved=0;saved<2;saved++)for(int frequency=0;frequency<2;frequency++)for(int angry=2;angry<4;angry++) {
            var d=Load(row);var variables=(List<DialogVariable>)d.GetType().GetField("_variables",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(d);variables.Add(new DialogVariable("yoga",yoga));
            foreach(var v in variables)if(v.name=="saved" || v.name=="frequency" || v.name=="angry")Set(v,"_value",v.name=="saved"?saved:v.name=="frequency"?frequency:angry);
            var story=AnnabelleStart(d,"annabelle-01-airport3",1);string text=string.Join("\n",Drain(story).ToArray());
            bool correct=HasText(text,"тайное место")== (yoga==1) && HasText(text,"не дают дышать")== (yoga==0 && saved==1);
            correct &= HasText(text,"Как после йоги, только")== (yoga==0 && saved==0 && frequency==1);
            correct &= HasText(text,"Из-за таких, как вы")== (yoga==0 && saved==0 && frequency==0 && angry>2);
            Check(correct,"Annabelle controlled external airport priority yoga/saved/frequency/angry "+yoga+"/"+saved+"/"+frequency+"/"+angry);
        }
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2381.json");
        Check((string)card["fullname"]=="Annabelle Robert"&&Convert.ToInt32(card["age"])==30&&Convert.ToInt32(card["gender"])==0,"Annabelle female30original sourcecard; photojournalist and caller relationships from dialogue, not name");
        var metadata=Strings(row["_user_datas"]);
        Check(metadata.Count==7&&metadata.Exists(x=>x.StartsWith("passengercall: ")&&x.Contains("«В аэропорт, пожалуйста»."))&&metadata.FindAll(x=>x.StartsWith("anim-link:")).Count==6,"Annabelle generic French airport request with original prefix and six untouched animations; English-only airport name not added");
    }

    static PrompterStory AgnesStart(Dialog d,string passage,int met) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Francois",met)},passage);return story;
    }
    static int AgnesRoutes(Dictionary<string,object> row,int met,List<int> route) {
        var story=AgnesStart(Load(row),"agnes-01-start",met);var actions=new List<string>();story.OnActionTriggered+=(a)=>actions.Add(a.action);
        var text=Drain(story);var selected=new List<string>();
        foreach(int answer in route) {
            var choices=story.GetChoices();if(answer>=choices.Count)throw new Exception("Agnes menu changed");
            selected.Add((string)choices[answer].GetType().GetField("_link",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(choices[answer]));
            story.ChooseAnswer(answer);text.AddRange(Drain(story));
        }
        int count=story.GetChoices().Count;
        if(count>0){if(route.Count>6)throw new Exception("Agnes loop");int total=0;for(int i=0;i<count;i++){var next=new List<int>(route);next.Add(i);total+=AgnesRoutes(row,met,next);}return total;}
        bool recognition=met==1 && selected.Contains("agnes-01-kids-surprise");
        int listen=(selected.Contains("agnes-01-beard-courage") || selected.Contains("agnes-01-beard-likeit")?1:0)+(selected.Contains("agnes-01-kids-scared")?1:0)+(selected.Contains("agnes-01-end-ok")?1:0);
        string all=string.Join("\n",text.ToArray());bool ok=story.FindVariable("listen").value==listen;
        ok &= HasText(all,"женщина. С бородой") && HasText(all,"дорожной косметички") && HasText(all,"двадцать лет") && HasText(all,"Четыре недели");
        ok &= HasText(all,"поздно прилетели") && !HasText(all,"После такого путешествия");
        ok &= HasText(all,"Ничего страшного")== (route[0]>0) && HasText(all,"Франсуа де ла Нере?")==recognition;
        ok &= HasText(all,"Вы представляете её идеальной бабушкой") && HasText(all,"Я никак не могла его отпустить");
        ok &= HasText(all,"Выгнать мужа из дома")==selected.Contains("agnes-01-end-ok");
        ok &= HasText(all,"А ведь моя старшая его обожает")== (recognition && !selected.Contains("agnes-01-kids-surprise-francois-freespeech"));
        ok &= HasText(all,"Не альтруистка. Не бескорыстная")==selected.Contains("agnes-01-kids-scared");
        ok &= actions.FindAll(a=>a.Contains("058_01")).Count==(selected.Contains("agnes-01-kids-surprise-francois-drive")?1:0);
        ok &= actions.FindAll(a=>a.Contains("058_02")).Count==(listen>2?1:0);
        var expectedTips=new List<string>{"medium"};
        if(selected.Contains("agnes-01-kids-surprise-francois-drive") || selected.Contains("agnes-01-kids-surprise-francois-bad"))expectedTips.Add("high");
        if(selected.Contains("agnes-01-kids-surprise-francois-freespeech"))expectedTips.Add("low");
        if(selected.Contains("agnes-01-kids-silence"))expectedTips.Add("medium");
        if(selected.Contains("agnes-01-end-silence"))expectedTips.Add("low");
        var tips=actions.FindAll(a=>a.Contains("tip"));ok &= tips.Count==expectedTips.Count;
        if(tips.Count==expectedTips.Count)for(int i=0;i<tips.Count;i++)ok &= tips[i].Contains(expectedTips[i]);
        ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
        ok &= actions.FindAll(a=>a.Contains("fare") || a.Contains("clue:")).Count==0 && !story.can_continue && story.GetChoices().Count==0;
        Check(ok,"Agnes complete route preserves woman, idioms, exhusband, Francois recognition and strict listen>2 "+met+"/"+string.Join(",",route));return 1;
    }
    static void AgnesChecks(Dictionary<string,object> row,Dictionary<string,object> francois) {
        int count=0;for(int met=0;met<2;met++)count+=AgnesRoutes(row,met,new List<int>());
        Check(count==264,"Agnes all full answer paths with/without prior Francois meeting: "+count);
        var isolated=AgnesStart(Load(row),"agnes-01-india-introB",0);string text=string.Join("\n",Drain(isolated).ToArray());
        Check(HasText(text,"После такого путешествия") && HasText(text,"Невероятно! Настоящее приключение"),"Agnes original unlinked introB translated, controlled direct start only");
        var first=Start(Load(francois),"francois-00a-start");Drain(first);
        var second=AgnesStart(Load(row),"agnes-01-kids-surprise",first.FindVariable("_Driver.Met.Francois").value);text=string.Join("\n",Drain(second).ToArray());
        Check(first.FindVariable("_Driver.Met.Francois").value==1 && HasText(text,"Франсуа де ла Нере?") && second.GetChoices().Count==4,"Francois actual introductory meeting flag opens Agnes's named recognition, not inferred political agreement");
        AgnesFrenchChecks(row);
    }

    static void AgnesFrenchChecks(Dictionary<string,object> row) {
        var reference=ReferenceFile("localization/agnes_fr_validation_routes.json");var seen=new HashSet<string>();var normal=new HashSet<string>();var edges=new HashSet<string>();var profileCounts=new int[4];int ordinary=0,controlled=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)r["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,edges);
            bool direct=Convert.ToBoolean(r["controlled"]);int met=Convert.ToInt32(((Dictionary<string,object>)r["globals"])["_Driver.Met.Francois"]);profileCounts[(direct?2:0)+met]++;
            if(direct)controlled++;else{ordinary++;foreach(string title in Strings(r["passages"]))normal.Add(title);}
            var path=Strings(r["passages"]);string all=string.Join("\n",text.ToArray());bool ok=!story.can_continue&&story.GetChoices().Count==0;
            if(path.Contains("agnes-01-india-introA"))ok &= all.Contains("Ничего страшного")&&!all.Contains("не умер");
            if(path.Contains("agnes-01-kids-surprise"))ok &= all.Contains("католических консерваторов старой школы")&&!all.Contains("по краям");
            if(path.Contains("agnes-01-beard-likeit2"))ok &= all.Contains("такое облегчение")&&all.Contains("Лёгкость. Такую лёгкость");
            if(path.Contains("agnes-01-kids-surprise-not"))ok &= all.Contains("Франсуа… как-то там")&&!all.Contains("Франсуа де ла Нере?");
            ok &= all.Contains("Может, завтра")&&all.Contains("на душе теплеет");
            Check(ok,"Agnes exact complete source history "+(direct?"controlled-introB ":"ordinary ")+met+"/"+(direct?controlled:ordinary)+" text/menu/state/orderedcommands, woman42/idioms/relief/uncertainty, no diagnosis or new relatives");
        }
        foreach(Dictionary<string,object> profile in (IEnumerable)reference["profiles"]) {
            bool direct=Convert.ToBoolean(profile["controlled"]);int met=Convert.ToInt32(profile["met"]);Check(profileCounts[(direct?2:0)+met]==Convert.ToInt32(profile["histories"]),"Agnes independent finite source profile count "+direct+"/"+met+" = "+profileCounts[(direct?2:0)+met]);
        }
        Check(ordinary==264&&controlled==88&&normal.Count==37&&seen.Count==38&&edges.Count==17&&Convert.ToInt32(reference["ordinary_histories"])==ordinary&&Convert.ToInt32(reference["controlled_histories"])==controlled,"Agnes all264ordinary plus88explicit source-unlinked introB continuations cover38blocks17answers; no new normal incoming link");
        bool menusOk=true;var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> block in (IEnumerable)row["_passages"])blocks[(string)block["_title"]]=block;
        foreach(Dictionary<string,object> menu in (IEnumerable)reference["menus"]) {
            var texts=Strings(menu["texts"]);var links=Strings(menu["links"]);int i=0;
            foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)menu["passage"]]["_choices"]){menusOk &= i<texts.Count&&texts[i]==(string)c["_text"]&&links[i]==(string)c["_link"];i++;}menusOk &= i==texts.Count;
        }
        Check(menusOk,"Agnes all17original answer meanings/order/links/ENcarrier icons retained, unqualified French surprise");
        var data=Strings(row["_user_datas"]);Check(data.Count==9&&data[0]=="passengercall: «Обратно в Булонь!»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==8,"Agnes useful French Boulogne call/no English-only please, original prefix/eight animations despite sourcehascall0; no icon drawings");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2382.json");Dictionary<string,object> encounter=null;foreach(Dictionary<string,object> item in (IEnumerable)card["encounters"]){encounter=item;break;}var content=(Dictionary<string,object>)encounter["dialog_content"];
        Check((string)card["fullname"]=="Agnès Bertrand"&&Convert.ToInt32(card["age"])==42&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(content["has_passenger_call"])==0&&Convert.ToInt32(content["has_travel"])==0,"Agnes original female42card and beard distinct from unnamed medical cause/gender history/profession/grandchildren; original hascall/travel0retained");
    }

    static void ClaudiaChecks(Dictionary<string,object> rows) {
        string ct;int clGame=0,clAttraction=0;
        for(int opening=0;opening<4;opening++)for(int drink=0;drink<4;drink++)for(int conclusion=0;conclusion<4;conclusion++) {
            if(opening==1 && drink==3 && conclusion!=0)continue;
            var d=Load((Dictionary<string,object>)rows["022_claudia_01"]);var cs=Start(d,"claudia-part1");var emitted=new List<string>();
            cs.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(cs);cs.ChooseAnswer(opening);ct=string.Join("\n",Drain(cs).ToArray());
            bool coherent=cs.FindVariable(opening==0?"whisky":(opening==1?"wine":"cocktail")).value==1;
            coherent &= ct.Contains(opening==0?"любите виски":(opening==1?"красное вино":"пьёт коктейли"));
            coherent &= cs.GetChoices().Count==4 && ct.Contains("Какой алкоголь мне нравится пить") && (ct.Contains("перекатывает на языке последний слог")== (opening>=2));
            cs.ChooseAnswer(drink);ct=string.Join("\n",Drain(cs).ToArray());
            int daring=(opening==0?8:0)+(drink==0?8:0),charming=(opening>=2?8:0)+(drink==1||drink==2?8:0);
            int insulting=(opening==1?8:0)+(drink==3?8:0);
            if(insulting==16) {
                coherent &= ct.Contains("пора прекратить игру") && cs.GetChoices().Count==0 && emitted.Exists(a=>a.Contains("tip") && a.Contains("low"));
                clGame++;Check(coherent,"Claudia two offending answers stop game before third round");continue;
            }
            coherent &= cs.GetChoices().Count==4;cs.ChooseAnswer(conclusion);ct=string.Join("\n",Drain(cs).ToArray());
            if(conclusion==0 || conclusion==1) {
                if(conclusion==0)daring+=8;else charming+=8;
                coherent &= ct.Contains("Не совсем так я себе это представляла")== (insulting>0);
                coherent &= ct.Contains("Вы были безупречны")== (insulting==0);
                coherent &= ct.Contains("Уверенно, но не агрессивно")== (daring==24);
                coherent &= ct.Contains("столько мягкости")== (charming==24);
                coherent &= ct.Contains("говорили так, что я почувствовала")== (daring==16);
                coherent &= ct.Contains("Несколько обаятельных слов")== (charming==16);
                coherent &= ct.Contains("не слишком удивила вас")== (daring<16 && charming<16);
                coherent &= emitted.Exists(a=>a.Contains("022_02")) && emitted.Exists(a=>a.Contains("tip") && a.Contains("high"));
                coherent &= emitted.Exists(a=>a.Contains("tip") && a.Contains("medium"))==(daring<16 && charming<16);
            }else if(conclusion==2) {
                coherent &= ct.Contains("прямо просила не") && ct.Contains("Я сказала: хватит") && cs.GetChoices().Count==0;
                coherent &= ct.Contains("КЛАУДИЯ: «Чёрт…»")== (insulting>=8);
                insulting+=8;coherent &= emitted.Exists(a=>a.Contains("tip") && a.Contains("low")) && !emitted.Exists(a=>a.Contains("022_02"));
            }else {
                coherent &= cs.GetChoices().Count==3 && ct.Contains("Я вам нравлюсь");
                coherent &= ct.Contains("Вы были таким напористым")== (daring>=16);
                coherent &= ct.Contains("Вы были таким обаятельным")== (daring<16 && charming>=16);
            }
            coherent &= cs.FindVariable("audace").value==daring && cs.FindVariable("charming").value==charming && cs.FindVariable("insulting").value==insulting;
            clGame++;Check(coherent,"Claudia drink guess, boundaries, nuanced thanks and original thresholds "+opening+"/"+drink+"/"+conclusion);
        }
        for(int opening=0;opening<4;opening++)for(int drink=0;drink<4;drink++)for(int attraction=0;attraction<3;attraction++) {
            if(opening==1 && drink==3)continue;
            var d=Load((Dictionary<string,object>)rows["022_claudia_01"]);var cs=Start(d,"claudia-part1");var emitted=new List<string>();
            cs.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(cs);cs.ChooseAnswer(opening);Drain(cs);cs.ChooseAnswer(drink);Drain(cs);
            cs.ChooseAnswer(3);Drain(cs);cs.ChooseAnswer(attraction);ct=string.Join("\n",Drain(cs).ToArray());
            bool coherent=emitted.Exists(a=>a.Contains("tip") && a.Contains("low")) && !emitted.Exists(a=>a.Contains("022_02"));
            coherent &= ct.Contains("Я миру безразлична")== (attraction==0);
            coherent &= ct.Contains("Вам тоже сделали больно")== (attraction==2);
            coherent &= ct.Contains("КЛАУДИЯ: «Что ж».")== (attraction==1);
            clAttraction++;Check(coherent,"Claudia all attraction replies after silence preserve distinct words and bad ending "+opening+"/"+drink+"/"+attraction);
        }
        foreach(int clarify in new int[]{0,1}) {
            var d=Load((Dictionary<string,object>)rows["022_claudia_01"]);var cs=Start(d,"claudia-intro");var emitted=new List<string>();
            cs.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(cs);cs.ChooseAnswer(clarify==0?2:1);
            if(clarify==1){Drain(cs);cs.ChooseAnswer(1);}
            ct=string.Join("\n",Drain(cs).ToArray());
            Check(ct.Contains("Я непростая пассажирка") && emitted.Exists(a=>a.Contains("tip") && a.Contains("low")),
                "Claudia direct refusal and refusal after clarification remain distinct from playing "+clarify);
        }
        var claudiaQuiet=Load((Dictionary<string,object>)rows["022_claudia_01"]);var cq=Start(claudiaQuiet,"claudia-start");Drain(cq);cq.ChooseAnswer(2);
        Drain(cq);cq.ChooseAnswer(2);ct=string.Join("\n",Drain(cq).ToArray());
        Check(ct.Contains("Сквозь слёзы") && !ct.Contains("пофлиртовали"),"Claudia continuous silence avoids imposing flirt game");
        foreach(int knows in new int[]{0,1}) {
            var d=Load((Dictionary<string,object>)rows["022_claudia_01"]);var cs=Start(d,"claudia-rules");Drain(cs);cs.ChooseAnswer(knows);
            ct=string.Join("\n",Drain(cs).ToArray());
            Check(ct.Contains("поражённую циррозом")== (knows==0) && cs.GetChoices().Count==4,
                "Claudia professional explanation is conditional and both choices reach game "+knows);
        }
        foreach(string killer in new string[]{"Judge","Sandman","Angel"}) {
            var d=Load((Dictionary<string,object>)rows["022_claudia_01"]);var cs=Flags(d,"claudia-accent-why",killer);var emitted=new List<string>();
            cs.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(cs).ToArray());
            Check(ct.Contains("родители умерли") && emitted.Exists(a=>a.Contains("clue_judge_customer_claudia_01")) && cs.FindVariable("_Found.Clue.Today").value==0,
                "Claudia parents statement preserves original unconditional clue command and no added flag "+killer);
        }
        var claudiaMixed=Load((Dictionary<string,object>)rows["022_claudia_01"]);
        var cm=Start(claudiaMixed,"claudia-part1");var mixedTips=new List<string>();
        cm.OnActionTriggered+=(evt)=>{if(evt.action.Contains("tip"))mixedTips.Add(evt.action);};
        Drain(cm);cm.ChooseAnswer(1);Drain(cm);cm.ChooseAnswer(1);Drain(cm);cm.ChooseAnswer(0);Drain(cm);
        Check(mixedTips.Count==2 && mixedTips[0].Contains("medium") && mixedTips[1].Contains("high"),
            "Claudia mixed positive route emits medium then high in original order");
        Check(clGame==61&&clAttraction==45,"Claudia all61original primary joint profiles and45attraction endings retained; intro/case/profession/tip controls preserved");
        var reference=ReferenceFile("localization/claudia_fr_validation_routes.json");var row=(Dictionary<string,object>)rows["022_claudia_01"];var covered=new HashSet<string>();var edges=new HashSet<string>();int complete=0;
        foreach(Dictionary<string,object> route in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,route,(string)reference["start"],MathieuGlobals(route["globals"]),out text,out actions);AngeliqueActions(route,actions);ReferenceCoverage(route,covered,edges);
            Check(!story.can_continue&&story.GetChoices().Count==0,"Claudia complete representative French text/source menus/state/orderedactions #"+(++complete));
        }
        Check(complete==70&&covered.Count==70&&edges.Count==42,"Claudia70complete source representative routes cover70blocks289lines42answers; equivalent prefixes merged, not every Cartesian prefix");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2351.json");
        Check((string)card["fullname"]=="Claudia Campos"&&Convert.ToInt32(card["age"])==46&&Convert.ToInt32(card["gender"])==0,"Claudia sourcecard female46; profession/origin/parents source levels separate, no new diagnosis");
        var metadata=Strings(row["_user_datas"]);
        Check(metadata.Count==5&&metadata.Exists(x=>x.StartsWith("passenger-call: ")&&x.Contains("Исси-ле-Мулино"))&&metadata.FindAll(x=>x.StartsWith("anim-link:")).Count==4,"Claudia nonstandard original passenger-call prefix translated; four animation definitions preserved");
    }
    static void MireilleChecks(Dictionary<string,object> row) {
        var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=50000000;
        var reference=serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText("localization/mireille_validation_routes.json",Encoding.UTF8));
        var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        int routeNumber=0;var covered=new HashSet<string>();var coveredAnswers=new HashSet<string>();
        foreach(Dictionary<string,object> route in (IEnumerable)reference["routes"]) {
            var story=new PrompterStory(Load(row),false);var actions=new List<string>();story.OnActionTriggered+=(a)=>actions.Add(a.action);
            story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Mireille",0)},"mireille-01-start");var text=Drain(story);
            var edges=new List<object>();foreach(object edge in (IEnumerable)route["edges"])edges.Add(edge);int edgeIndex=0;
            foreach(object answer in (IEnumerable)route["answers"]) {
                var edge=new List<object>();foreach(object value in (IEnumerable)edges[edgeIndex++])edge.Add(value);
                string title=(string)edge[0];var expectedMenu=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[title]["_choices"])expectedMenu.Add(c);
                var choices=story.GetChoices();int index=Convert.ToInt32(answer);
                if(choices.Count!=expectedMenu.Count || index!=Convert.ToInt32(edge[1]))throw new Exception("Mireille source menu count/edge changed");
                for(int i=0;i<choices.Count;i++) {
                    var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
                    if((string)choices[i].GetType().GetField("_text",flags).GetValue(choices[i])!=(string)expectedMenu[i]["_text"] || (string)choices[i].GetType().GetField("_link",flags).GetValue(choices[i])!=(string)expectedMenu[i]["_link"])throw new Exception("Mireille source menu text/link changed");
                }
                coveredAnswers.Add(title+"/"+index);story.ChooseAnswer(index);text.AddRange(Drain(story));
            }
            var expected=Strings(route["expected_texts"]);bool ok=text.Count==expected.Count;
            if(text.Count==expected.Count)for(int i=0;i<text.Count;i++)ok &= text[i]==expected[i];
            AngeliqueActions(route,actions);
            foreach(var pair in (Dictionary<string,object>)route["expected_variables"])ok &= story.FindVariable(pair.Key)!=null && story.FindVariable(pair.Key).value==Convert.ToInt32(pair.Value);
            var passages=new HashSet<string>(Strings(route["passages"]));foreach(string title in passages)covered.Add(title);
            bool bridge=passages.Contains("mireille-01-part1-accept"),bridgeOut=passages.Contains("mireille-01-part1-out"),cemetery=passages.Contains("mireille-01-part2-intro2-accept"),walk=passages.Contains("mireille-01-part2-leave"),alexandre=passages.Contains("mireille-01-part3-albatros-alexandre");
            int time=(bridge?1:0)+(cemetery?1:0);string all=string.Join("\n",text.ToArray());
            ok &= story.FindVariable("time").value==time && story.FindVariable("part1-out").value==(bridgeOut?2:bridge?1:0) && story.FindVariable("part2-out").value==0;
            ok &= story.FindVariable("_Driver.Met.Mireille").value==1;
            ok &= HasText(all,"Последний…") && !HasText(all,"22:12") && HasText(all,"терактов") && HasText(all,"Цветы апельсина");
            ok &= HasText(all,"убираете документы")==passages.Contains("mireille-01-part2-stay");
            ok &= HasText(all,"Под мостом Мирабо течёт Сена")==bridgeOut && HasText(all,"мама в конторе, папа собирал")==bridgeOut;
            ok &= HasText(all,"немного богемный")==cemetery && !HasText(all,"нетрадиционной ориентации");
            ok &= HasText(all,"Мой муж и любовник")==walk && HasText(all,"Люк не прожил и двух месяцев")==walk;
            ok &= HasText(all,"два мужских имени")==walk && HasText(all,"убирает сухую ветку")==walk;
            ok &= HasText(all,"внука при пожаре")==alexandre && HasText(all,"Пожертвовал молодостью ради Франции")==alexandre && HasText(all,"договорить вашу ложь")==alexandre && !HasText(all,"Индокитае");
            ok &= HasText(all,"Мой поезд… Через три минуты")== (time==2) && HasText(all,"Мой поезд… Через несколько минут")== (time==1);
            ok &= HasText(all,"Заднее сиденье кажется непривычно пустым")== (time>0) && HasText(all,"Оно кажется пустее обычного")== (time==0);
            ok &= HasText(all,"Позади вас раздаётся свисток к отправлению поезда")== (time>0);
            var tips=actions.FindAll(a=>a.Contains("tip"));var wantedTips=new List<string>{"none"};if(walk)wantedTips.Add("low");if(passages.Contains("mireille-01-part2-cemetary2-sorry"))wantedTips.Add("medium");
            ok &= tips.Count==wantedTips.Count;if(tips.Count==wantedTips.Count)for(int i=0;i<tips.Count;i++)ok &= tips[i].Contains(wantedTips[i]);
            var destination=actions.FindAll(a=>a.Contains("destination"));var wantedDestinations=new List<int>();if(bridge){wantedDestinations.Add(0);wantedDestinations.Add(2);}if(cemetery){wantedDestinations.Add(1);if(walk)wantedDestinations.Add(2);}
            ok &= destination.Count==wantedDestinations.Count;if(destination.Count==wantedDestinations.Count)for(int i=0;i<destination.Count;i++)ok &= destination[i].Contains(wantedDestinations[i].ToString());
            ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1+(bridge?1:0)+(cemetery?1:0);
            ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1+(bridge?1:0)+(cemetery?1:0);
            ok &= actions.FindAll(a=>a.Contains("060_01")).Count==(bridge?1:0) && actions.FindAll(a=>a.Contains("060_02")).Count==(bridgeOut?1:0);
            ok &= actions.FindAll(a=>a.Contains("060_03")).Count==(cemetery?1:0) && actions.FindAll(a=>a.Contains("060_04")).Count==(walk?1:0);
            ok &= actions.FindAll(a=>a.Contains("060_05")).Count==(alexandre?1:0) && actions.FindAll(a=>a.Contains("060_06")).Count==(time>0?1:0) && actions.FindAll(a=>a.Contains("060_07")).Count==(time==0?1:0);
            ok &= actions.FindAll(a=>a.Contains("clue_sand_customer_mireille_01")).Count==(alexandre?1:0);
            ok &= actions.FindAll(a=>a.Contains("fare")).Count==0 && HasText(all,"расплачивается") && !story.can_continue && story.GetChoices().Count==0;
            Check(ok,"Mireille full representative route preserves exact lines/menus/state/actions, French documents/bag, two men, fire testimony, clue and original carrier train/destination behavior "+(++routeNumber)+" time="+time);
        }
        Check(covered.Count==87 && coveredAnswers.Count==67 && routeNumber==207,"Mireille complete representative routes cover87passages/67answers/102menu states; equivalent prefixes merged, not every Cartesian path");
    }

    static void ContextualSecondChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/contextual_second_validation_routes.json");
        int number=0,passageCount=0,answerCount=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];var row=(Dictionary<string,object>)rows[name];
            var covered=new HashSet<string>();var answers=new HashSet<string>();
            foreach(Dictionary<string,object> route in (IEnumerable)scene["routes"]) {
                List<string> text,actions;var story=ReferenceRun(row,route,(string)route["start"],MathieuGlobals(route["globals"]),out text,out actions);
                AngeliqueActions(route,actions);ReferenceCoverage(route,covered,answers);
                Check(!story.can_continue && story.GetChoices().Count==0,"Second contextual FR exact full source witness/text/menu/state/orderedactions "+name+" "+(++number));
            }
            Check(covered.Count==Convert.ToInt32(scene["passages_covered"]) && answers.Count==Convert.ToInt32(scene["answers_covered"]),"Second contextual FR all reachable passages/answer edges "+name);
            passageCount+=covered.Count;answerCount+=answers.Count;
        }
        Check(number==178 && passageCount==97 && answerCount==59,"Second contextual FR178complete witnesses/97compiled blocks59answers; Herve3single-case profiles plus no-case/mixed controls, not all Cartesian histories or Unity");
    }

    static void FrenchNavigationChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/french_navigation_validation_routes.json");
        int ordinary=0,controlled=0,totalPassages=0,totalAnswers=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];var row=(Dictionary<string,object>)rows[name];
            var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
            foreach(var pair in (Dictionary<string,object>)scene["menus"]) {
                var actual=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[pair.Key]["_choices"])actual.Add(c);
                var expected=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)pair.Value)expected.Add(c);
                bool menu=actual.Count==expected.Count;
                if(menu)for(int i=0;i<actual.Count;i++)menu &= (string)actual[i]["_text"]==(string)expected[i]["_text"] && (string)actual[i]["_link"]==(string)expected[i]["_link"] && Convert.ToInt32(actual[i]["_ending"])==Convert.ToInt32(expected[i]["_ending"]);
                Check(menu,"French navigation immutable-source menu text/target/order/ending "+name+" "+pair.Key);
            }
            var covered=new HashSet<string>();var answers=new HashSet<string>();
            foreach(string kind in new string[]{"routes","controlled_routes"})foreach(Dictionary<string,object> route in (IEnumerable)scene[kind]) {
                var globals=new List<DialogVariable>();foreach(var pair in (Dictionary<string,object>)route["globals"])globals.Add(new DialogVariable(pair.Key,Convert.ToInt32(pair.Value)));
                List<string> text,actions;var story=ReferenceRun(row,route,(string)route["start"],globals,out text,out actions);AngeliqueActions(route,actions);
                var path=new HashSet<string>(Strings(route["passages"]));bool ok=!story.can_continue && story.GetChoices().Count==0;
                if(name=="017_grace_01") {
                    bool bad=path.Contains("grace-01-end-bad");var tips=actions.FindAll(a=>Regex.IsMatch(a,@"^tip\s*:"));
                    ok &= tips.Count==1 && tips[0].Contains(bad?"low":"medium");
                    ok &= actions.FindAll(a=>Regex.IsMatch(a,@"^newspaper\s*:\s*1$")).Count==(bad?1:0);
                    ok &= path.Contains("customer-newspaper")==bad && !path.Contains(bad?"grace-01-end":"grace-01-end-bad");
                }
                if(name=="018_francois_07" && kind=="routes")ok &= !path.Contains("francois-07-silence");
                if(name=="020_jacquie_01") {
                    bool clue=Convert.ToInt32(((Dictionary<string,object>)route["globals"])["_Jacquie.Clue.Dialogue"])==1;
                    ok &= story.FindVariable("_Jacquie.Clue.Dialogue").value==0;
                    ok &= actions.FindAll(a=>Regex.IsMatch(a,@"^fare\s*:\s*0$")).Count==(clue?1:0);
                }
                if(name=="060_mireille_01" && path.Contains("mireille-01-part2-intro2-refuse")) {
                    bool bridge=path.Contains("mireille-01-part1-accept");
                    ok &= path.Contains("mireille-01-part2-intro2-refuseA")==bridge && path.Contains("mireille-01-part2-intro2-refuseB")==!bridge;
                    ok &= !path.Contains("mireille-01-part2-intro2-accept") && story.FindVariable("time").value==(bridge?1:0);
                }
                if(kind=="routes"){ordinary++;ReferenceCoverage(route,covered,answers);}else controlled++;
                Check(ok,"French navigation exact complete source witness/text/state/ordered actions "+name+" "+kind+" "+(ordinary+controlled));
            }
            Check(covered.Count==Convert.ToInt32(scene["passages_covered"]) && answers.Count==Convert.ToInt32(scene["answers_covered"]),"French navigation all ordinary source passages/answer edges "+name);
            totalPassages+=covered.Count;totalAnswers+=answers.Count;
        }
        Check(ordinary==1325 && controlled==2 && totalPassages==156 && totalAnswers==118,"French navigation 1118finite ordinary histories plus207previous Mireille profiles and2separate source-unlinked controls; not all Cartesian Mireille/Unity");
    }

    static PrompterStory ReferenceRun(Dictionary<string,object> row,Dictionary<string,object> route,string start,List<DialogVariable> globals,out List<string> text,out List<string> actions,bool compareText=true) {
        var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        var story=new PrompterStory(Load(row),false);var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);actions=emitted;
        story.StartDialog(globals,start);text=Drain(story);var edges=new List<object>();foreach(object edge in (IEnumerable)route["edges"])edges.Add(edge);int edgeIndex=0;
        foreach(object answer in (IEnumerable)route["answers"]) {
            var edge=new List<object>();foreach(object value in (IEnumerable)edges[edgeIndex++])edge.Add(value);string title=(string)edge[0];
            var expectedMenu=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[title]["_choices"])expectedMenu.Add(c);
            var choices=story.GetChoices();int index=Convert.ToInt32(answer);
            if(choices.Count!=expectedMenu.Count || index!=Convert.ToInt32(edge[1]))throw new Exception(start+" source menu count/edge changed");
            for(int i=0;i<choices.Count;i++) {
                var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
                if((string)choices[i].GetType().GetField("_text",flags).GetValue(choices[i])!=(string)expectedMenu[i]["_text"] || (string)choices[i].GetType().GetField("_link",flags).GetValue(choices[i])!=(string)expectedMenu[i]["_link"])throw new Exception(start+" source menu text/link changed");
            }
            story.ChooseAnswer(index);text.AddRange(Drain(story));
        }
        if(compareText) {
            var expected=Strings(route["expected_texts"]);if(text.Count!=expected.Count)throw new Exception(start+" full visible line count changed");
            for(int i=0;i<text.Count;i++)if(text[i]!=expected[i])throw new Exception(start+" full visible line order changed at "+i);
        }
        foreach(var pair in (Dictionary<string,object>)route["expected_variables"])if(story.FindVariable(pair.Key)==null || story.FindVariable(pair.Key).value!=Convert.ToInt32(pair.Value))throw new Exception(start+" reference variable mismatch "+pair.Key);
        return story;
    }
    static void ReferenceCoverage(Dictionary<string,object> route,HashSet<string> passages,HashSet<string> answers) {
        foreach(string title in Strings(route["passages"]))passages.Add(title);
        foreach(object edge in (IEnumerable)route["edges"]) {var values=new List<object>();foreach(object value in (IEnumerable)edge)values.Add(value);answers.Add((string)values[0]+"/"+Convert.ToInt32(values[1]));}
    }
    static Dictionary<string,object> ReferenceFile(string path) {
        var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=50000000;return serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText(path,Encoding.UTF8));
    }
    static void DenisChecks(Dictionary<string,object> row) {
        var reference=ReferenceFile("localization/denis_validation_routes.json");var covered=new HashSet<string>();var answers=new HashSet<string>();int number=0;
        foreach(Dictionary<string,object> route in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,route,"denis-01-start",null,out text,out actions);
            ReferenceCoverage(route,covered,answers);var p=new HashSet<string>(Strings(route["passages"]));string all=string.Join("\n",text.ToArray());
            bool admitted=p.Contains("denis-01-sorry-yes"),tv=p.Contains("denis-01-sorry-tv"),talk=p.Contains("denis-01-end-notracist"),luck=p.Contains("denis-01-end-goodluck");
            bool ok=story.FindVariable("drivers-knows-show").value==(admitted || tv?1:0);
            ok &= HasText(all,"больничной койки") && HasText(all,"деревенский акцент") && HasText(all,"Твитэр") && HasText(all,"Интырнет");
            ok &= HasText(all,"Жена умерла двадцать пять лет назад")==!admitted && HasText(all,"Поля любви")==!admitted;
            ok &= HasText(all,"шесть фермеров")==tv;
            ok &= HasText(all,"Говорят, я девственник и нацист") && HasText(all,"Ещё — что я своих коров") && HasText(all,"двоюродному брату");
            ok &= HasText(all,"не… расист")==talk && HasText(all,"Глупости")==p.Contains("denis-01-end-notracist-whatdidyousay");
            ok &= HasText(all,"Хочу извинений")==p.Contains("denis-01-end-speech") && HasText(all,"не хотел так разглядывать")==p.Contains("denis-01-sorry-intro");
            ok &= HasText(all,"газету") && HasText(all,"Или, может, кем-то раньше?") && HasText(all,"расплачивается");
            var tips=actions.FindAll(a=>a.Contains("tip"));ok &= tips.Count==(talk?2:1) && tips[0].Contains("low") && (!talk || tips[1].Contains("high"));
            ok &= actions.FindAll(a=>a.Contains("061_01")).Count==1 && actions.FindAll(a=>a.Contains("061_02")).Count==(luck?1:0) && actions.FindAll(a=>a.Contains("061_03")).Count==(talk?1:0);
            ok &= actions.FindAll(a=>a.Contains("newspaper")).Count==1;
            ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
            ok &= actions.FindAll(a=>a.Contains("clue:") || a.Contains("fare") || a.Contains("destination")).Count==0 && !story.can_continue && story.GetChoices().Count==0;
            Check(ok,"Denis full representative route preserves unreliable TV claims, intentional slang, knowledge-flag branches and uncertain newspaper owner "+(++number));
        }
        Check(number==34 && covered.Count==35 && answers.Count==29,"Denis34complete representative routes cover35passages/29answers/15menu states; not all Cartesian paths");
        DenisFrenchChecks(row);
    }
    static int DenisAllHistories(Dictionary<string,object> row,List<int> route) {
        var story=Start(Load(row),"denis-01-start");var actions=new List<string>();story.OnActionTriggered+=(evt)=>actions.Add(evt.action);Drain(story);var selected=new List<string>();
        foreach(int answer in route){var choices=story.GetChoices();if(answer>=choices.Count)throw new Exception("Denis full-history menu changed");selected.Add((string)choices[answer].GetType().GetField("_link",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(choices[answer]));story.ChooseAnswer(answer);Drain(story);}
        int count=story.GetChoices().Count;if(count>0){if(route.Count>12)throw new Exception("Denis full-history loop");int total=0;for(int i=0;i<count;i++){var next=new List<int>(route);next.Add(i);total+=DenisAllHistories(row,next);}return total;}
        bool talk=selected.Contains("denis-01-end-speech")||selected.Contains("denis-01-end-goodluck"),luck=selected.Contains("denis-01-end-goodluck"),knows=selected.Contains("denis-01-sorry-yes")||selected.Contains("denis-01-sorry-tv");
        bool ok=story.FindVariable("drivers-knows-show").value==(knows?1:0)&&!story.can_continue&&story.GetChoices().Count==0;
        var tips=actions.FindAll(x=>x.Contains("tip"));ok &= tips.Count==(talk?2:1)&&tips[0].Contains("low")&&(!talk||tips[1].Contains("high"));
        ok &= actions.FindAll(x=>x.Contains("061_00")).Count==1&&actions.FindAll(x=>x.Contains("061_01")).Count==1&&actions.FindAll(x=>x.Contains("061_02")).Count==(luck?1:0)&&actions.FindAll(x=>x.Contains("061_03")).Count==(talk?1:0);
        ok &= actions.FindAll(x=>x.Contains("newspaper")).Count==1&&actions.FindAll(x=>x.Contains("travel")&&x.Contains("start")).Count==1&&actions.FindAll(x=>x.Contains("travel")&&x.Contains("stop")).Count==1&&actions.FindAll(x=>x.Contains("clue:")||x.Contains("fare")||x.Contains("destination")).Count==0;
        if(!ok)throw new Exception("Denis full immutable history final state/actions differ: "+string.Join(",",route));return 1;
    }
    static void DenisFrenchChecks(Dictionary<string,object> row) {
        var reference=ReferenceFile("localization/denis_fr_validation_routes.json");var covered=new HashSet<string>();var edges=new HashSet<string>();int rides=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,"denis-01-start",null,out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,covered,edges);var path=Strings(r["passages"]);string all=string.Join("\n",text.ToArray());bool ok=!story.can_continue&&story.GetChoices().Count==0;
            ok &= all.Contains("Он щурится")&&all.Contains("поехал в Париж")&&all.Contains("Мэмы? Мемы! Мумы?")&&all.Contains("возвращаетесь к работе");
            if(path.Contains("denis-01-sorry-tv"))ok &= all.Contains("Значит, вы знаете, кто я?");
            if(path.Contains("denis-01-ourtimes"))ok &= all.Contains("широко раскрывает глаза")&&all.Contains("световые годы");
            if(path.Contains("denis-01-end-notracist-media"))ok &= all.Contains("Пальцы дрожат");
            Check(ok,"Denis exact French representative "+(++rides)+" text/menu/state/orderedcommands; widened eyes/positive TV phrase/rumor attribution/explicit accent adaptation, no new kinship or diagnosis");
        }
        Check(rides==34&&covered.Count==35&&edges.Count==29&&Convert.ToInt32(reference["state_nodes"])==56&&Convert.ToInt32(reference["menu_states"])==15,"Denis all34archived exact witnesses/56states15menus cover35blocks29answers with original ordered actions");
        int histories=DenisAllHistories(row,new List<int>());Check(histories==6048&&histories==Convert.ToInt32(reference["full_answer_histories"]),"Denis all6048finite native complete answer histories independently counted, exact final knowledge/tips/reveal/news/travel states; not6048independent text snapshots");
        bool menusOk=true;var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> block in (IEnumerable)row["_passages"])blocks[(string)block["_title"]]=block;
        foreach(Dictionary<string,object> menu in (IEnumerable)reference["menus"]){var texts=Strings(menu["texts"]);var links=Strings(menu["links"]);int i=0;foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)menu["passage"]]["_choices"]){menusOk &= i<texts.Count&&texts[i]==(string)c["_text"]&&links[i]==(string)c["_link"];i++;}menusOk &= i==texts.Count;}
        Check(menusOk,"Denis all29French-aligned answer meanings/order/targets and existing icons, no new image files");
        var data=Strings(row["_user_datas"]);Check(data.Count==7&&data[0]=="passengercall: «В Плен-Сен-Дени, пожалуйста»."&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==6,"Denis correct original useful request/carrier prefix/six animations unchanged despite sourcehascall0");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2385.json");Dictionary<string,object> encounter=null;foreach(Dictionary<string,object> item in (IEnumerable)card["encounters"]){encounter=item;break;}var content=(Dictionary<string,object>)encounter["dialog_content"];
        Check((string)card["fullname"]=="Denis Petitjean"&&Convert.ToInt32(card["age"])==49&&Convert.ToInt32(card["gender"])==3&&Convert.ToInt32(content["has_passenger_call"])==0&&Convert.ToInt32(content["has_travel"])==0,"Denis original male49card, source farm/widower testimony, surname not proved kinship; original card flags retained, not Unity acceptance");
    }

    static List<DialogVariable> PaulineFlags(int mode) {
        var globals=new List<DialogVariable>();string[] names={"Angel","Judge","Sandman"};for(int i=0;i<names.Length;i++)globals.Add(new DialogVariable("_Killer.Is."+names[i],mode==i?1:0));return globals;
    }
    static void PaulineChecks(Dictionary<string,object> row) {
        var reference=ReferenceFile("localization/pauline_validation_routes.json");var covered=new HashSet<string>();var answers=new HashSet<string>();int number=0;
        foreach(Dictionary<string,object> route in (IEnumerable)reference["routes"]) {
            int mode=Convert.ToInt32(route["mode"]);List<string> text,actions;var story=ReferenceRun(row,route,"pauline-01-start",PaulineFlags(mode),out text,out actions);
            ReferenceCoverage(route,covered,answers);var p=new HashSet<string>(Strings(route["passages"]));string all=string.Join("\n",text.ToArray());
            bool philosophical=p.Contains("pauline-01-love3-badchoices"),shrug=p.Contains("pauline-01-love5-dontcare"),help=p.Contains("pauline-01-driver-helpme"),good=help && mode<3;
            bool ok=story.FindVariable("choice").value==(philosophical?1:0);
            ok &= HasText(all,"Неверные побуждения")==philosophical && HasText(all,"После вашей-то небольшой речи о выборе")== (philosophical && shrug);
            ok &= HasText(all,"Журналистка, как и я") && HasText(all,"Живёт в Сиэтле") && HasText(all,"уже совсем не обращали на это внимания") && HasText(all,"Четыре дня бок о бок");
            ok &= HasText(all,"отгулы") && HasText(all,"пересадка в Лондоне") && HasText(all,"едете обратно в аэропорт");
            ok &= HasText(all,"расчёски нет")==shrug && HasText(all,"всегда ношу паспорт")==shrug;
            ok &= HasText(all,"Я веду своё расследование")==help && HasText(all,"О Бюссе, конечно, умалчиваете")==help && HasText(all,"стене у себя дома")==help;
            ok &= HasText(all,"Он пришлёт вам то, что знает об Ангеле смерти")== (help && mode==0);
            ok &= HasText(all,"Он пришлёт вам всё, что знает о Судье")== (help && mode==1);
            ok &= HasText(all,"Он пришлёт вам всё, что знает о Песочном человеке")== (help && mode==2);
            ok &= HasText(all,"связи наверху")== (help && mode==1);
            ok &= HasText(all,"опустошить счёт")==good && HasText(all,"Я сделала выбор")==!good;
            var tips=actions.FindAll(a=>a.Contains("tip"));ok &= tips.Count==1 && tips[0].Contains("medium");
            ok &= actions.FindAll(a=>a.Contains("063_01")).Count==(p.Contains("pauline-01-love2-summerthing")?1:0) && actions.FindAll(a=>a.Contains("063_02")).Count==(p.Contains("pauline-01-love4-run-no")?1:0);
            ok &= actions.FindAll(a=>a.Contains("063_03")).Count==1 && actions.FindAll(a=>a.Contains("063_04")).Count==(help?1:0);
            ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==(help && mode==3?2:1);
            ok &= actions.FindAll(a=>a.Contains("clue:") || a.Contains("destination") || a.Contains("fare")).Count==0 && HasText(all,"Расплачивается и выходит") && !story.can_continue && story.GetChoices().Count==0;
            Check(ok,"Pauline full representative route preserves woman lover/male contact, comp time, every uturn and three separate case promises "+(++number)+" mode="+mode);
        }
        Check(number==116 && covered.Count==49 && answers.Count==29,"Pauline116complete representative routes cover49passages/29answers/64menu states in three cases and no case; not all Cartesian paths");
        for(int mask=0;mask<8;mask++) {
            var globals=new List<DialogVariable>();string[] names={"Angel","Judge","Sandman"};for(int i=0;i<3;i++)globals.Add(new DialogVariable("_Killer.Is."+names[i],(mask&(1<<i))!=0?1:0));
            var story=new PrompterStory(Load(row),false);var actions=new List<string>();story.OnActionTriggered+=(a)=>actions.Add(a.action);story.StartDialog(globals,"pauline-01-driver-helpme");string text=string.Join("\n",Drain(story).ToArray());
            int selected=(mask&1)!=0?0:(mask&2)!=0?1:(mask&4)!=0?2:3;
            bool ok=HasText(text,"об Ангеле смерти")== (selected==0) && HasText(text,"о Судье")== (selected==1) && HasText(text,"о Песочном человеке")== (selected==2);
            ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==(selected==3?2:1);
            Check(ok,"Pauline controlled mixed case flags preserve original Angel/Judge/Sandman priority and no-case duplicate stop "+mask);
        }
        PaulineFrenchChecks(row);
    }

    static void PaulineFrenchChecks(Dictionary<string,object> row) {
        var reference=ReferenceFile("localization/pauline_fr_validation_routes.json");var seen=new HashSet<string>();var edges=new HashSet<string>();var profiles=new int[8];int rides=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,"pauline-01-start",MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,edges);int mode=Convert.ToInt32(r["mode"]);profiles[mode]++;
            var path=Strings(r["passages"]);bool help=path.Contains("pauline-01-driver-helpme"),philosophical=path.Contains("pauline-01-love3-badchoices"),shrug=path.Contains("pauline-01-love5-dontcare");var flags=(Dictionary<string,object>)r["globals"];bool angel=Convert.ToInt32(flags["_Killer.Is.Angel"])==1,judge=Convert.ToInt32(flags["_Killer.Is.Judge"])==1,sand=Convert.ToInt32(flags["_Killer.Is.Sandman"])==1;int selected=angel?0:judge?1:sand?2:3;
            string all=string.Join("\n",text.ToArray());bool ok=!story.can_continue&&story.GetChoices().Count==0&&story.FindVariable("choice").value==(philosophical?1:0);
            ok &= all.Contains("она хочет выговориться")&&all.Contains("НЕОБХОДИМО выговориться")&&all.Contains("не обращали на это внимания")&&all.Contains("скрывается в холле терминала");
            if(path.Contains("pauline-01-love2-youthink"))ok &= all.Contains("Не собиралась же я с ней уезжать")&&all.Contains("это ничего не значит");
            ok &= all.Contains("Я веду своё расследование")==help&&HasText(all,"После вашей-то небольшой речи о выборе")== (philosophical&&shrug);
            ok &= all.Contains("Он пришлёт вам то, что знает об Ангеле смерти")== (help&&selected==0)&&all.Contains("Он пришлёт вам всё, что знает о Судье")== (help&&selected==1)&&all.Contains("Он пришлёт вам всё, что знает о Песочном человеке")== (help&&selected==2);
            ok &= actions.FindAll(x=>x.Contains("travel")&&x.Contains("start")).Count==1&&actions.FindAll(x=>x.Contains("travel")&&x.Contains("stop")).Count==(help&&selected==3?2:1)&&actions.FindAll(x=>x.Contains("063_03")).Count==1&&actions.FindAll(x=>x.Contains("063_04")).Count==(help?1:0);
            ok &= actions.FindAll(x=>x.Contains("clue:")||x.Contains("fare")||x.Contains("destination")).Count==0;
            Check(ok,"Pauline complete exact French witness "+(++rides)+" profile="+mode+" desire/need/owninvestigation, women/contact promise, allcase priority and source double-stop preserved; orderedcommands checked");
        }
        int total=0;foreach(Dictionary<string,object> profile in (IEnumerable)reference["history_counts"]){int mode=Convert.ToInt32(profile["mode"]);Check(profiles[mode]==29&&profiles[mode]==Convert.ToInt32(profile["representatives"])&&Convert.ToInt32(profile["histories"])==17280,"Pauline source graph census17280histories/profile "+mode+" with29complete native representatives; not allCartesian executions");total+=Convert.ToInt32(profile["histories"]);}
        Check(rides==232&&seen.Count==49&&edges.Count==29&&Convert.ToInt32(reference["state_nodes"])==564&&Convert.ToInt32(reference["menu_states"])==128&&total==138240&&total==Convert.ToInt32(reference["total_histories"]),"Pauline all8masks/232source witnesses/49blocks29answers/564states128menus, independent138240history census not138240native runs");
        bool menusOk=true;var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> block in (IEnumerable)row["_passages"])blocks[(string)block["_title"]]=block;
        foreach(Dictionary<string,object> menu in (IEnumerable)reference["menus"]){var texts=Strings(menu["texts"]);var links=Strings(menu["links"]);int i=0;foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)menu["passage"]]["_choices"]){menusOk &= i<texts.Count&&texts[i]==(string)c["_text"]&&links[i]==(string)c["_link"];i++;}menusOk &= i==texts.Count;}
        Check(menusOk,"Pauline all29French-aligned answers/order/targets and source icons including original ENsilence marker preserved, punctuation repaired without new artwork");
        var data=Strings(row["_user_datas"]);Check(data.Count==7&&data[0]=="passengercall: «Ла-Мотт-Пике…»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==6,"Pauline initial LaMottePicquet request/sixanimations unchanged, later airport is separate narrative direction; no new icons");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2386.json");Dictionary<string,object> encounter=null;foreach(Dictionary<string,object> item in (IEnumerable)card["encounters"]){encounter=item;break;}var content=(Dictionary<string,object>)encounter["dialog_content"];
        Check((string)card["fullname"]=="Pauline Hoareau"&&Convert.ToInt32(card["age"])==34&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(content["has_passenger_call"])==0&&Convert.ToInt32(content["has_travel"])==0,"Pauline original female34card/amoureuse, loverwoman and distinct unnamed contact, no name-based sex or inferred futurewife identity; not Unity acceptance");
    }

    static void JanetChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/janet_validation_routes.json");
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];var row=(Dictionary<string,object>)rows[name];var covered=new HashSet<string>();var edges=new HashSet<string>();int total=0;
            int rounds=name.EndsWith("_04")?2:1;
            for(int round=0;round<rounds;round++)foreach(object group in (IEnumerable)scene["groups"]) {
                var alternatives=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> r in (IEnumerable)group)alternatives.Add(r);
                var first=alternatives[0];var globals=new List<DialogVariable>();foreach(var pair in (Dictionary<string,object>)first["globals"])globals.Add(new DialogVariable(pair.Key,Convert.ToInt32(pair.Value)));
                List<string> text,actions;string start=null;foreach(Dictionary<string,object> passage in (IEnumerable)row["_passages"]){start=(string)passage["_title"];break;}
                var story=ReferenceRun(row,first,start,globals,out text,out actions,false);Dictionary<string,object> matched=null;
                foreach(var candidate in alternatives) {
                    var expected=Strings(candidate["expected_texts"]);bool same=expected.Count==text.Count;
                    if(same)for(int i=0;i<text.Count;i++)same &= text[i]==expected[i];if(same){matched=candidate;break;}
                }
                if(matched==null)throw new Exception("Janet output not an allowed exact full source sequence "+name);
                ReferenceCoverage(matched,covered,edges);var p=new HashSet<string>(Strings(matched["passages"]));string all=string.Join("\n",text.ToArray());
                bool ok=!story.can_continue && story.GetChoices().Count==0 && story.FindVariable("_Driver.Met.Janet").value==1;
                ok &= actions.FindAll(a=>a.Contains("fare") || a.Contains("destination")).Count==0;
                if(name.EndsWith("_00a")) {
                    ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==2 && actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
                    ok &= HasText(all,"резко тормозите")==p.Contains("janet-00a-nopark") && HasText(all,"Не кричи");
                } else if(name.EndsWith("_01")) {
                    ok &= HasText(all,"Большая часть населения заражена") && HasText(all,"В 2037 году") && HasText(all,"Хёга возглавляет");
                    ok &= actions.FindAll(a=>a.Contains("064_04")).Count==Convert.ToInt32(((Dictionary<string,object>)first["globals"])["_Driver.Met.AliceHyoga"]);
                } else if(name.EndsWith("_02")) {
                    int met=Convert.ToInt32(((Dictionary<string,object>)first["globals"])["_Driver.Met.Sean"]);
                    ok &= HasText(all,"Шон Джексон") && HasText(all,"Аннабель Робер") && HasText(all,"поехать в Сирию");
                    ok &= actions.FindAll(a=>a.Contains("064_05")).Count==met && actions.FindAll(a=>a.Contains("064_06")).Count==1;
                    ok &= HasText(all,"Меня отправили не в тот момент")==p.Contains("janet-02-sean-no");
                } else if(name.EndsWith("_03")) {
                    ok &= HasText(all,"Полин Оаро") && HasText(all,"в Канаде") && HasText(all,"будущей женой");
                } else if(name.EndsWith("_04")) {
                    ok &= actions.FindAll(a=>a.Contains("064_08")).Count==(p.Contains("janet-04-message-clear-04")?1:0);
                    ok &= !HasText(all,"двадцать лет назад") && actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
                } else if(name.EndsWith("_05")) {
                    ok &= HasText(all,"записали") && HasText(all,"нащупывают картонную папку") && HasText(all,"Мерсси, J.") && HasText(all,"два года позже сегодняшней") && !HasText(all,"Спасибо, Джей");
                    var tokens=Regex.Matches(string.Join("\n",actions.ToArray()),@"clue_(?:judge|sand|angel)_journal\d+_\d+");
                    int wanted=p.Contains("janet-05-clue-judge")?11:p.Contains("janet-05-clue-sand") || p.Contains("janet-05-clue-angel")?10:0;ok &= tokens.Count==wanted;
                    ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1 && actions.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==0;
                    ok &= actions.FindAll(a=>a.Contains("064_09")).Count==1;
                }
                Check(ok,"Janet exact native full answer path preserves timeline claims, name/source discrepancies, radio randomness and case papers "+name+"/"+(++total)+"/round"+round);
            }
            Check(covered.Count==Convert.ToInt32(scene["passages_covered"]) && edges.Count==Convert.ToInt32(scene["answers_covered"]),"Janet every default reachable passage and answer covered in actual native runs "+name+" passages="+covered.Count+" answers="+edges.Count+" paths="+total);
        }
        var isolated=Start(Load((Dictionary<string,object>)rows["064_janet_01"]),"janet-01-seenhyoga-A-dontknow");string ct=string.Join("\n",Drain(isolated).ToArray());
        Check(HasText(ct,"Постарайся вспомнить") && HasText(ct,"Хитоми"),"Janet unlinked A-dontknow original block: controlled direct start only");
        for(int mask=0;mask<8;mask++) {
            var globals=new List<DialogVariable>();string[] names={"Judge","Sandman","Angel"};for(int i=0;i<3;i++)globals.Add(new DialogVariable("_Killer.Is."+names[i],(mask&(1<<i))!=0?1:0));
            var story=new PrompterStory(Load((Dictionary<string,object>)rows["064_janet_05"]),false);var actions=new List<string>();story.OnActionTriggered+=(a)=>actions.Add(a.action);story.StartDialog(globals,"janet-05-end");Drain(story);
            var tokens=Regex.Matches(string.Join("\n",actions.ToArray()),@"clue_(?:judge|sand|angel)_journal\d+_\d+");string expected=(mask&1)!=0?"judge":(mask&2)!=0?"sand":(mask&4)!=0?"angel":"";
            bool ok=tokens.Count==(expected=="judge"?11:expected==""?0:10);foreach(Match match in tokens)ok &= match.Value.StartsWith("clue_"+expected+"_");
            Check(ok,"Janet controlled final case flags preserve Judge/Sandman/Angel priority and exact11/10/10clue set "+mask);
        }
        foreach(string who in new string[]{"Hyoga","Sean"}) {
            var first=who=="Hyoga"?Start(Load((Dictionary<string,object>)rows["024_alicehyoga_01"]),"alicehyoga-start"):SeanStart(Load((Dictionary<string,object>)rows["036_sean_01"]));Drain(first);
            int turns=0;while(first.GetChoices().Count>0){if(++turns>30)throw new Exception("Related Janet meeting loop");first.ChooseAnswer(0);Drain(first);}
            string flag=who=="Hyoga"?"_Driver.Met.AliceHyoga":"_Driver.Met.Sean";int actual=first.FindVariable(flag).value;
            var second=new PrompterStory(Load((Dictionary<string,object>)rows[who=="Hyoga"?"064_janet_01":"064_janet_02"]),false);second.OnActionTriggered+=(a)=>{};
            second.StartDialog(new List<DialogVariable>{new DialogVariable(flag,actual)},who=="Hyoga"?"janet-01-start":"janet-02-start");ct=string.Join("\n",Drain(second).ToArray());
            Check(actual==1 && HasText(ct,who=="Hyoga"?"Считал себя оборотнем":"Встречал его"),"Janet actual completed related passenger meeting unlocks named recall "+who);
        }
    }

    static string CustomerCase(Dictionary<string,object> globals,bool thoughts) {
        if(Convert.ToInt32(globals["_Killer.Is.Judge"])==1)return "judge";
        string second=thoughts?"Angel":"Sandman",third=thoughts?"Sandman":"Angel";
        if(Convert.ToInt32(globals["_Killer.Is."+second])==1)return second=="Angel"?"angel":"sandman";
        if(Convert.ToInt32(globals["_Killer.Is."+third])==1)return third=="Angel"?"angel":"sandman";
        return "none";
    }
    static bool CustomerClueActions(List<string> actions,string kind) {
        var clues=actions.FindAll(a=>a.Contains("clue:"));if(kind=="none")return clues.Count==0;
        if(clues.Count!=1)return false;string prefix=kind=="sandman"?"sand":kind;int count=kind=="angel"?14:10;
        if(clues[0].Split(',').Length!=count)return false;
        for(int i=1;i<=count;i++)if(!clues[0].Contains("clue_"+prefix+"_customer_rumor_"+i.ToString("00")))return false;
        return true;
    }
    static void SonnyAntoineChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/sonny_antoine_validation_routes.json");int total=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];var row=(Dictionary<string,object>)rows[name];var seen=new HashSet<string>();var edges=new HashSet<string>();int number=0;
            foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"]) {
                var globals=(Dictionary<string,object>)r["globals"];List<string> text,actions;var story=ReferenceRun(row,r,(string)scene["start"],MathieuGlobals(globals),out text,out actions);
                ReferenceCoverage(r,seen,edges);var p=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",text.ToArray());var tips=actions.FindAll(a=>a.Contains("tip:"));
                bool ok=actions.FindAll(a=>a.Contains("travel: start")).Count==1 && actions.FindAll(a=>a.Contains("travel: stop")).Count==1 && actions.FindAll(a=>a.Contains("destination:") || a.Contains("fare:")).Count==0;
                if(name=="068_sonny_01") {
                    ok &= HasText(all,"игрокиней") && !HasText(all,"игроман") && HasText(all,"никто не заставляет вас их заводить") && HasText(all,"объясню ему напрямую");
                    ok &= HasText(all,"бретонские гречневые блины") && HasText(all,"словно кошка") && !HasText(all,"игроком-мужчиной");
                    ok &= tips.Count==1 && tips[0].Contains("low") && CustomerClueActions(actions,"none") && MathieuValue(story,"_Driver.Met.Sonny")==0;
                } else if(name=="068_sonny_02") {
                    bool dayOne=Convert.ToInt32(globals["_Day.Number"])==1,smoke=Convert.ToInt32(globals["_Driver.Is.Smoking"])==0;
                    string clue=dayOne?"none":CustomerCase(globals,false),thought=dayOne?"none":CustomerCase(globals,true);
                    ok &= p.Contains("customer-add-clue")==!dayOne && CustomerClueActions(actions,clue) && MathieuValue(story,"_Found.Clue.Today")== (clue=="none"?7:1);
                    foreach(string k in new string[]{"judge","sandman","angel"})ok &= p.Contains("sonny-02-firstapproach-"+k)== (thought==k);
                    ok &= HasText(all,"Нестерпимо хочется закурить")==smoke && HasText(all,"Лицо совсем незнакомо") && HasText(all,"Вы правда славный парень");
                    ok &= !HasText(all,"пиратскую") && tips.Count==1 && tips[0].Contains("high") && MathieuValue(story,"_Driver.Met.Sonny")==0;
                } else {
                    bool early=p.Contains("antoine-01-smalltalk-killer"),old=p.Contains("antoine-01-old"),restaurant=p.Contains("antoine-01-whatif-ifiwin-restaurant"),spend=p.Contains("antoine-01-whatif-ifiwin-spenditall"),friends=p.Contains("antoine-01-whatif-ifiwin-share"),away=p.Contains("antoine-01-whatif-ifiwin-away");
                    string clue=early?CustomerCase(globals,false):"none";
                    ok &= CustomerClueActions(actions,clue) && MathieuValue(story,"_Found.Clue.Today")== (clue=="none"?7:1) && MathieuValue(story,"told-concierge")== (early?1:0);
                    ok &= p.Contains("antoine-01-smalltalk-outro-told-concierge")==!early && (clue=="none" || p.Contains("antoine-01-smalltalk-killer2-outro"));
                    foreach(string k in new string[]{"restaurant","spenditall","friends","goaway"}) {
                        bool value=k=="restaurant"?restaurant:k=="spenditall"?spend:k=="friends"?friends:away;
                        ok &= MathieuValue(story,"wish-"+k)== (value?1:0);
                    }
                    string reply=restaurant?"restaurant":spend?"spenditall":friends?"friends":away?"goaway":"friends";
                    foreach(string k in new string[]{"restaurant","spenditall","friends","goaway"})ok &= p.Contains("antoine-01-old-"+k)== (old&&reply==k);
                    ok &= HasText(all,"семьдесят четыре")==old && HasText(all,"Жена давно умерла")==p.Contains("antoine-01-old-friends");
                    ok &= HasText(all,"Решил работать в финансах")==p.Contains("antoine-01-whereisthemoney-justfifty") && HasText(all,"долгами и кредитом")==p.Contains("antoine-01-whatif-ifiwin");
                    ok &= HasText(all,"Четыреста тридцать четыре миллиона евро") && HasText(all,"пятидесяти тысяч евро") && HasText(all,"в Португалии") && HasText(all,"левые из высшего света");
                    ok &= tips.Count==1 && tips[0].Contains("high") && HasText(all,"на востоке Парижа");
                }
                Check(ok,"Sonny/Antoine full representative native route exact text/menu/state, source rumors, meta address or conditional wishes "+name+"/"+(++number));total++;
            }
            Check(seen.Count==Convert.ToInt32(scene["passages_covered"]) && edges.Count==Convert.ToInt32(scene["answers_covered"]),"Sonny/Antoine complete source-menu coverage "+name+" routes="+number+" passages="+seen.Count+" answers="+edges.Count);
        }
        Check(total==1373,"Sonny/Antoine1373complete representative paths cover91paragraphs/44answers across3scenes, not every Cartesian prefix");
        var reviewed=ReferenceFile("localization/contexts/068_sonny_ui.json");var ui=ReferenceFile("../translation/ui_ru.json");var mapping=ReferenceFile("revision/data/Russian_UI/full_translation_mapping.json");int keys=0;
        foreach(Dictionary<string,object> k in (IEnumerable)reviewed["keys"]) {
            if((string)ui[(string)k["key"]]!=(string)k["russian"] || (string)mapping[(string)k["english"]]!=(string)k["russian"])throw new Exception("Sonny customer-rumor reviewed UI mismatch");keys++;
        }
        Check(keys==68 && (string)ui["clue_judge_customer_rumor_07.title"]=="Жертву №2 никогда не арестовывали за убийство алжирца" && ((string)ui["clue_angel_customer_rumor_14.title"]).Contains("внешность безобидная"),"Sonny/Antoine68shared rumor UIkeys match reviewed EN/FR subjects, source and correctiondata; not liveUI acceptancetest");
    }

    static void MarcPierretteChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/marc_pierrette_validation_routes.json");int total=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];var row=(Dictionary<string,object>)rows[name];var seen=new HashSet<string>();var edges=new HashSet<string>();int number=0;
            foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"]) {
                var globals=(Dictionary<string,object>)r["globals"];List<string> text,actions;var story=ReferenceRun(row,r,(string)scene["start"],MathieuGlobals(globals),out text,out actions);
                ReferenceCoverage(r,seen,edges);var p=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",text.ToArray());var tips=actions.FindAll(a=>a.Contains("tip:"));bool ok=!story.can_continue && story.GetChoices().Count==0;
                ok &= actions.FindAll(a=>a.Contains("destination:")||a.Contains("fare:")).Count==0;
                int known=Convert.ToInt32(globals["_Pierrette.Restaurant.Known"]);
                if(name=="071_marcsophie_01") {
                    bool warm=p.Contains("maso-01-heatA"),hope=p.Contains("maso-01-end-marc-hope"),luck=p.Contains("maso-01-end-sophie-courage");
                    ok &= MathieuValue(story,"marc")== (warm?1:0) && MathieuValue(story,"sophie")== (warm?0:1) && MathieuValue(story,"_Pierrette.Restaurant.Known")==1;
                    ok &= p.Contains("maso-01-start-restaurantA")== (known==1) && p.Contains("maso-01-start-restaurantB")== (known==0);
                    ok &= p.Contains("maso-01-street-marc")==warm && p.Contains("maso-01-end-sophie")==warm;
                    ok &= p.Contains("maso-01-street-sophie")==!warm && p.Contains("maso-01-end-marc")==!warm;
                    ok &= HasText(all,"поднимается по ступеням отеля")==warm;
                    ok &= HasText(all,"при детях")==warm && HasText(all,"дети поймут")==hope;
                    ok &= HasText(all,"оставшейся без руководителя") && HasText(all,"Я сейчас не в своей тарелке") && HasText(all,"пятьдесят метров") && HasText(all,"ещё немного сидите в такси молча");
                    ok &= tips.Count== (luck?2:1) && tips[0].Contains("medium") && (!luck||tips[1].Contains("low"));
                    ok &= actions.FindAll(a=>a.Contains("travel:") && a.Contains("start")).Count==2 && actions.FindAll(a=>a.Contains("travel:") && a.Contains("stop")).Count==2;
                    ok &= actions.FindAll(a=>a.Contains("clue:")).Count==0 && MathieuValue(story,"_Found.Clue.Today")==7;
                } else {
                    bool upstairs=p.Contains("pierrette-01-end-upstairs"),wine=p.Contains("pierrette-01-end-upstairs-wine"),truth=p.Contains("pierrette-01-marseille-truth"),lie=p.Contains("pierrette-01-marseille-lie");
                    ok &= MathieuValue(story,"_Driver.Met.Pierrette")==1 && MathieuValue(story,"_Pierrette.Restaurant.Known")==known;
                    ok &= p.Contains("pierrette-01-whoiam-knowrestaurant")== (known==1) && p.Contains("pierrette-01-whoiam-dontknow")== (known==0);
                    ok &= MathieuValue(story,"_Pierrette.Upstairs")== (upstairs?1:0) && MathieuValue(story,"wine")== (wine?1:0);
                    ok &= HasText(all,"Ла Тимон")==truth && HasText(all,"Обань")==lie && !(truth&&lie);
                    ok &= HasText(all,"груша конфи с имбирём")==upstairs && !HasText(all,"гивашит") && !HasText(all,"Пропащая") && HasText(all,"Она опускает глаза");
                    ok &= HasText(all,"Час спустя")==upstairs && HasText(all,"два бокала")==wine && HasText(all,"Голова чуть кружится")==wine;
                    ok &= p.Contains("pierrette-01-end-downstairs2")== (upstairs&&!wine) && p.Contains("pierrette-01-end-taxi")==!upstairs;
                    ok &= actions.FindAll(a=>a.Contains("clue:")).Count== (upstairs?1:0) && actions.FindAll(a=>a.Contains("clue_angel_customer_pierrette_01")).Count== (upstairs?1:0);
                    ok &= MathieuValue(story,"_Found.Clue.Today")== (upstairs?1:7) && tips.Count==2 && tips[0].Contains("medium") && tips[1].Contains(upstairs?"none":"high");
                    ok &= actions.FindAll(a=>a.Contains("travel:") && a.Contains("start")).Count==1 && actions.FindAll(a=>a.Contains("travel:") && a.Contains("stop")).Count==1;
                }
                Check(ok,"Marc/Pierrette full representative exact native text/menu/state, known restaurant and conditional passenger finale "+name+"/"+(++number));total++;
            }
            Check(seen.Count==Convert.ToInt32(scene["passages_covered"]) && edges.Count==Convert.ToInt32(scene["answers_covered"]),"Marc/Pierrette complete source-menu coverage "+name+" passages="+seen.Count+" answers="+edges.Count);
        }
        Check(total==96,"Marc/Pierrette96complete representative routes cover72passages/35answers; not every Cartesian prefix or Unity case selection");
        var checkedUI=ReferenceFile("localization/contexts/072_pierrette_ui.json");var ui=ReferenceFile("../translation/ui_ru.json");var mapping=ReferenceFile("revision/data/Russian_UI/full_translation_mapping.json");int keys=0;
        foreach(Dictionary<string,object> k in (IEnumerable)checkedUI["keys"]) {if((string)ui[(string)k["key"]]!=(string)k["russian"]||(string)mapping[(string)k["english"]]!=(string)k["russian"])throw new Exception("Pierrette UI differs from reviewed text");keys++;}
        Check(keys==3 && ((string)ui["clue_angel_customer_pierrette_01.title"]).Contains("Пьеретт ничего не сказала") && ((string)ui["072_pierrette.bio"]).Contains("Финансовые трудности"),"Marc/Pierrette3reviewed dossier/clue UIkeys, canonical indeclinable name, scar absence and unchanged source money discrepancy");
        var scenes=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> s in (IEnumerable)reference["scenes"])scenes.Add(s);
        foreach(string heat in new string[]{"maso-01-heatA","maso-01-heatB"}) {
            Dictionary<string,object> before=null,after=null;
            foreach(Dictionary<string,object> r in (IEnumerable)scenes[0]["routes"])if(Convert.ToInt32(r["mode"])==0 && Strings(r["passages"]).Contains(heat)){before=r;break;}
            foreach(Dictionary<string,object> r in (IEnumerable)scenes[1]["routes"])if(Convert.ToInt32(r["mode"])==1){after=r;break;}
            List<string> t,a;var previous=ReferenceRun((Dictionary<string,object>)rows["071_marcsophie_01"],before,(string)scenes[0]["start"],MathieuGlobals(before["globals"]),out t,out a);
            var g=new Dictionary<string,object>((Dictionary<string,object>)after["globals"]);g["_Pierrette.Restaurant.Known"]=MathieuValue(previous,"_Pierrette.Restaurant.Known");
            var next=ReferenceRun((Dictionary<string,object>)rows["072_pierrette_01"],after,(string)scenes[1]["start"],MathieuGlobals(g),out t,out a);
            Check(MathieuValue(previous,"_Pierrette.Restaurant.Known")==1 && HasText(string.Join("\n",t.ToArray()),"Слышал о нём") && !HasText(string.Join("\n",t.ToArray()),"Не знаю такого"),"Marc/Pierrette actual completed couple route passes learned restaurant flag to chef encounter "+heat);
        }
    }

    static void MyriamMenu(PrompterStory story,Dictionary<string,object> row,string title,int answer) {
        Dictionary<string,object> block=null;foreach(Dictionary<string,object> b in (IEnumerable)row["_passages"])if((string)b["_title"]==title)block=b;
        var wanted=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)block["_choices"])wanted.Add(c);
        var actual=story.GetChoices();if(actual.Count!=wanted.Count||answer<0||answer>=actual.Count)throw new Exception("Myriam native menu count changed "+title);
        var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        for(int i=0;i<actual.Count;i++)if((string)actual[i].GetType().GetField("_text",flags).GetValue(actual[i])!=(string)wanted[i]["_text"]||(string)actual[i].GetType().GetField("_link",flags).GetValue(actual[i])!=(string)wanted[i]["_link"])throw new Exception("Myriam native menu text/link changed "+title);
        story.ChooseAnswer(answer);
    }
    static PrompterStory MyriamRandomRun(Dictionary<string,object> row,Dictionary<string,object> policy,List<DialogVariable> globals,out Dictionary<string,object> matched,out List<string> text,out List<string> actions) {
        var d=Load(row);if(policy.ContainsKey("initial_tell"))foreach(var v in (List<DialogVariable>)d.GetType().GetField("_variables",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(d))if(v.name=="tell")Set(v,"_value",Convert.ToInt32(policy["initial_tell"]));
        var story=new PrompterStory(d,false);var emitted=new List<string>();story.OnActionTriggered+=(a)=>emitted.Add(a.action);actions=emitted;
        story.StartDialog(globals,policy.ContainsKey("start")?(string)policy["start"]:"myriam-01-start");text=Drain(story);
        var candidates=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> r in (IEnumerable)policy["candidates"])candidates.Add(r);
        var edges=new List<object>();foreach(object e in (IEnumerable)candidates[0]["edges"])edges.Add(e);int index=0;
        foreach(object answer in (IEnumerable)policy["prefix"]) {
            var values=new List<object>();foreach(object v in (IEnumerable)edges[index++])values.Add(v);MyriamMenu(story,row,(string)values[0],Convert.ToInt32(answer));text.AddRange(Drain(story));
        }
        if(story.GetChoices().Count>0){MyriamMenu(story,row,"myriam-01-changeofheart",Convert.ToInt32(policy["station_answer"]));text.AddRange(Drain(story));}
        if(story.can_continue||story.GetChoices().Count>0)throw new Exception("Myriam random route did not finish");matched=null;
        foreach(var r in candidates) {
            var wanted=Strings(r["expected_texts"]);bool equal=wanted.Count==text.Count;if(equal)for(int i=0;i<text.Count;i++)if(wanted[i]!=text[i])equal=false;
            if(equal){foreach(var pair in (Dictionary<string,object>)r["expected_variables"])if(MathieuValue(story,pair.Key)!=Convert.ToInt32(pair.Value))throw new Exception("Myriam random variables changed");matched=r;break;}
        }
        if(matched==null)throw new Exception("Myriam native random text is outside independently generated EN outcomes");return story;
    }
    static bool MyriamAngelMeaning(PrompterStory story,Dictionary<string,object> r,List<string> text,List<string> actions,bool controlled) {
        var ps=new HashSet<string>(Strings(r["passages"]));bool station=ps.Contains("myriam-01-end-tell");string all=string.Join("\n",text.ToArray());var tips=actions.FindAll(a=>a.Contains("tip:"));bool ok=tips.Count==1 && tips[0].Contains(station?"mid":"low");
        ok &= actions.FindAll(a=>a.Contains("travel:")&&a.Contains("start")).Count==(controlled?0:1) && actions.FindAll(a=>a.Contains("travel:")&&a.Contains("stop")).Count==1 && actions.FindAll(a=>a.Trim()=="movie: stop").Count==(station?1:0);
        ok &= actions.FindAll(a=>a.Contains("clue:")||a.Contains("destination:")).Count==0 && HasText(all,"входит в участок")==station;
        ok &= HasText(all,"Позвоню маме")==ps.Contains("myriam-01-changeofheart-mother") && HasText(all,"медленно расцветающую")==ps.Contains("myriam-01-changeofheart-stay");
        if(!controlled) {
            ok &= HasText(all,"Все бёдра в синяках") && !HasText(all,"Новая пассажирка") && !HasText(all,"серийным убийцей");
            ok &= HasText(all,"шкаф, где он хранит обувь")==ps.Contains("myriam-01-story03") && MathieuValue(story,"tell")== (ps.Contains("myriam-01-story03")?2:1);
            ok &= ps.Contains("myriam-01-choice-2")== (MathieuValue(story,"tell")==2) && !ps.Contains("myriam-01-choice-3");
        } else ok &= MathieuValue(story,"tell")==3 && ps.Contains("myriam-01-choice-3");
        return ok;
    }
    static void MyriamChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/myriam_validation_routes.json");int deterministic=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];var seen=new HashSet<string>();var edges=new HashSet<string>();int number=0;
            foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"]) {
                List<string> text,actions;var g=(Dictionary<string,object>)r["globals"];var story=ReferenceRun((Dictionary<string,object>)rows[name],r,(string)scene["start"],MathieuGlobals(g),out text,out actions);ReferenceCoverage(r,seen,edges);
                var ps=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",text.ToArray());bool ok=!story.can_continue && story.GetChoices().Count==0;
                ok &= actions.FindAll(a=>a.Contains("destination:")).Count==0;
                if(name.EndsWith("00a")) {
                    string clue=CustomerCase(g,false);ok &= CustomerClueActions(actions,clue) && MathieuValue(story,"_Found.Clue.Today")== (clue=="none"?7:1);
                    ok &= actions.FindAll(a=>a.Contains("travel:")&&a.Contains("start")).Count==1 && actions.FindAll(a=>a.Contains("travel:")&&a.Contains("stop")).Count==0;
                    ok &= HasText(all,"иранской семье") && HasText(all,"новые клиенты") && HasText(all,"одним приятелем")==ps.Contains("myriam-00-question-no");
                } else if(name.EndsWith("02")) {
                    int interest=MathieuValue(story,"interest");bool shame=ps.Contains("myriam-02-shame"),rejection=ps.Contains("myriam-02-rejection");
                    ok &= interest>=0&&interest<=4 && ps.Contains("myriam-02-end-reveal")== (interest>2) && ps.Contains("myriam-02-end-revealno")== (interest<=2);
                    ok &= HasText(all,"девятнадцать")==shame && HasText(all,"Я ещё учусь. Живу с мамой")==shame && !HasText(all,"учусь в школе");
                    ok &= HasText(all,"Он… за вами ухаживал")==rejection && HasText(all,"влюблена в отца ребёнка") && HasText(all,"несколько парней, одна девушка");
                    var tips=actions.FindAll(a=>a.Contains("tip:"));ok &= tips.Count== (shame?1:0) && (!shame||tips[0].Contains("mid"));
                    ok &= actions.FindAll(a=>a.Contains("travel:")&&a.Contains("start")).Count==0 && actions.FindAll(a=>a.Contains("travel:")&&a.Contains("stop")).Count==1;
                    ok &= actions.FindAll(a=>a.Contains("clue:")).Count==0;
                } else {
                    bool truth=ps.Contains("myriam-03-story04-truth"),think=ps.Contains("myriam-03-story04-think");
                    ok &= HasText(all,"Вы лжёте.\n«Нет».") && HasText(all,"родился волком")==think && HasText(all,"старшем брате")==truth && HasText(all,"Он умер")==truth;
                    ok &= HasText(all,"Последняя маленькая ложь")==truth && HasText(all,"мысли целиком заняты… братом") && HasText(all,"с тринадцати лет");
                    ok &= !HasText(all,"Вообще-то, нет") && HasText(all,"Фигурку из мультфильма") && HasText(all,"канцелярские");
                    var tips=actions.FindAll(a=>a.Contains("tip:"));ok &= tips.Count==1 && tips[0].Contains("mid") && actions.FindAll(a=>a.Contains("clue:")).Count==0;
                    ok &= actions.FindAll(a=>a.Contains("travel:")&&a.Contains("start")).Count==0 && actions.FindAll(a=>a.Contains("travel:")&&a.Contains("stop")).Count==1;
                }
                Check(ok,"Myriam deterministic exact full route and conditional narrative facts "+name+"/"+(++number));deterministic++;
            }
            Check(seen.Count==Convert.ToInt32(scene["passages_covered"])&&edges.Count==Convert.ToInt32(scene["answers_covered"]),"Myriam deterministic complete menu-state coverage "+name);
        }
        Check(deterministic==71,"Myriam71representative deterministic routes, shared intro and separate Judge/Sandman stories; not sequential families");
        var angel=(Dictionary<string,object>)reference["angel"];var row=(Dictionary<string,object>)rows["073_myriam_01"];var covered=new HashSet<string>();var answers=new HashSet<string>();int policies=0,journeys=0;
        foreach(Dictionary<string,object> policy in (IEnumerable)angel["plans"]) {
            var outcomes=new HashSet<string>();int tries=0;
            while(outcomes.Count<2 && tries++<100) {
                System.Threading.Thread.Sleep(20);Dictionary<string,object> matched;List<string> text,actions;var story=MyriamRandomRun(row,policy,null,out matched,out text,out actions);
                var ps=new HashSet<string>(Strings(matched["passages"]));outcomes.Add(ps.Contains("myriam-01-end-tell")?"station":"home");ReferenceCoverage(matched,covered,answers);
                Check(MyriamAngelMeaning(story,matched,text,actions,false),"MyriamAngel actual full native random route exact independent text/menu/state and police/home facts "+(++journeys));
            }
            Check(outcomes.Count==2,"MyriamAngel both actual weighted outcomes observed for representative prefix/tail policy "+(++policies));
        }
        Check(policies==69&&covered.Count==37&&answers.Count==25,"MyriamAngel actual full journeys="+journeys+" policies=69 cover37reachable blocks/25answers, tellmax2; choice3 controlled separately");
        var controlled=(Dictionary<string,object>)angel["controlled"];var tails=new HashSet<string>();int controlledTries=0;
        while(tails.Count<2&&controlledTries++<200) {
            System.Threading.Thread.Sleep(20);Dictionary<string,object> matched;List<string> text,actions;var story=MyriamRandomRun(row,controlled,null,out matched,out text,out actions);bool station=Strings(matched["passages"]).Contains("myriam-01-end-tell");tails.Add(station?"station":"home");
            if(!MyriamAngelMeaning(story,matched,text,actions,true))throw new Exception("Myriam controlled tell3 wrong text/actions");
        }
        Check(tails.Count==2,"MyriamAngel source-unreachable tell3 controlled weighted jump observes both tails with preserved9candidates and exact text/actions");
        var ui=ReferenceFile("../translation/ui_ru.json");var mapping=ReferenceFile("revision/data/Russian_UI/full_translation_mapping.json");var old=ReferenceFile("localization/contexts/068_sonny_ui.json");int keys=0;
        foreach(Dictionary<string,object> k in (IEnumerable)old["keys"]) {if((string)ui[(string)k["key"]]!=(string)k["russian"]||(string)mapping[(string)k["english"]]!=(string)k["russian"])throw new Exception("Myriam shared rumor UI changed");keys++;}
        Check(keys==68,"Myriam shared34rumors/68UIkeys retain already reviewed subjects and case-specific testimony, not simultaneous clue distribution");
    }

    static void EmiliaChecks(Dictionary<string,object> row) {
        var reference=ReferenceFile("localization/emilia_validation_routes.json");Dictionary<string,object> scene=null;foreach(Dictionary<string,object> s in (IEnumerable)reference["scenes"])scene=s;
        var seen=new HashSet<string>();var edges=new HashSet<string>();int number=0;
        foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)scene["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,edges);
            var p=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",text.ToArray());bool driver=p.Contains("emilia-01-cat-driverkills02"),client=p.Contains("emilia-01-cat-clientkills02"),kill=p.Contains("emilia-01-cat-driverkills02-kill"),parents=p.Contains("emilia-01-truth-more"),job=p.Contains("emilia-01-start-youare"),depart=p.Contains("emilia-01-end-drive");bool ok=!story.can_continue && story.GetChoices().Count==0;
            ok &= driver!=client && MathieuValue(story,"reveal")== (client?1:0) && MathieuValue(story,"job")== (job?1:0) && MathieuValue(story,"_Driver.Met.Emilia")==1;
            ok &= p.Contains("emilia-01-truth-university")==job && p.Contains("emilia-01-truth-student")==!job;
            ok &= !p.Contains("emilia-01-cat-go02")||client;
            ok &= HasText(all,"Кис-кис-кис") && HasText(all,"Венсен") && HasText(all,"когда этот… проснулся") && !HasText(all,"Petit") && !HasText(all,"Vincennes");
            ok &= HasText(all,"магазинчик — сигареты и газеты")==parents && HasText(all,"МОЙ кот") && HasText(all,"университета");
            ok &= HasText(all,"облако смога")==driver && HasText(all,"Пальцы хотят сигарету")==driver && HasText(all,"Как учил брат")==kill;
            ok &= HasText(all,"кровь на песке во дворе")==kill;
            if(p.Contains("emilia-01-start-outro"))ok &= text.Contains("Она еле слышно бормочет «простите», на миг закрывает глаза и наслаждается теплом.");
            if(p.Contains("emilia-01-cat-silence"))ok &= text.Contains("ЭМИЛИЯ: «Я облажалась… Мы, наверное, ошиблись с дозировкой…»");
            if(kill)ok &= text.Contains("Вы резко поворачиваете его голову.")&&text.Contains("Вы снова видите кровь на песке во дворе… Она течёт к вашим ногам…");
            ok &= HasText(all,"этот семестр")==p.Contains("emilia-01-story02");
            if(p.Contains("emilia-01-story04"))ok &= text.Contains("ЭМИЛИЯ: «Повсюду».")&&HasText(all,"репутация потом преследует повсюду");
            if(p.Contains("emilia-01-responsibility"))ok &= text.Contains("ЭМИЛИЯ: «На самом деле я редко беру на себя ответственность».");
            ok &= HasText(all,"Согласилась на помощь одного ассистента")==p.Contains("emilia-01-story02") && HasText(all,"он ещё дышит")==p.Contains("emilia-01-story03");
            ok &= HasText(all,"Я подам жалобу")==p.Contains("emilia-01-story04") && HasText(all,"он всё снял")==p.Contains("emilia-01-story04");
            ok &= HasText(all,"вам показалось, что видите брата")==p.Contains("emilia-01-stopworrying") && HasText(all,"бывших комнат для прислуги")==!depart && HasText(all,"ёбаная ночь")==depart;
            ok &= !HasText(all,"комнат старой девы") && !HasText(all,"школьной лаборатории") && !HasText(all,"продолжать идти") && HasText(all,"без слов расплачивается");
            var tips=actions.FindAll(a=>a.Contains("tip:"));var wanted=new List<string>{"low"};if(driver)wanted.Add("high");if(p.Contains("emilia-01-responsibility")||p.Contains("emilia-01-stoptalking"))wanted.Add("none");
            ok &= tips.Count==wanted.Count;if(tips.Count==wanted.Count)for(int i=0;i<tips.Count;i++)ok &= tips[i].Contains(wanted[i]);
            var destinations=actions.FindAll(a=>a.Contains("destination:"));ok &= destinations.Count==1 && destinations[0].Contains("0");
            ok &= actions.FindAll(a=>a.Contains("travel:")&&a.Contains("start")).Count==1 && actions.FindAll(a=>a.Contains("travel:")&&a.Contains("stop")).Count==1 && actions.FindAll(a=>a.Contains("clue:")).Count==0;
            ok &= actions.FindAll(a=>a.Contains("074_03")).Count== (client?1:0) && actions.FindAll(a=>a.Contains("074_02")).Count== (driver?1:0);
            Check(ok,"Emilia full representative exact native text/menu/state, conditional lies/cat action/later speech and original tips/destination "+(++number));
        }
        Check(number==70&&seen.Count==48&&edges.Count==42,"Emilia70representative full routes cover48passages/42answers/41source menu states; not every Cartesian prefix");
        var proof=ReferenceFile("review/emilia_killer_flags.json");var values=(Dictionary<string,object>)proof["values"];var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2396.json");bool enumOK=Convert.ToBoolean(proof["flags_attribute"]) && (string)proof["value3"]=="judge, sandman" && Convert.ToInt32(values["judge"])==1 && Convert.ToInt32(values["sandman"])==2 && Convert.ToInt32(values["angel"])==4 && Convert.ToInt32(card["related_to_killers"])==4;
        foreach(Dictionary<string,object> e in (IEnumerable)card["encounters"])enumOK &= Convert.ToInt32(e["appears_only_for_killer"])==3;
        using(var hash=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead((string)proof["assembly"]))enumOK &= BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()==(string)proof["assembly_sha256"];
        Check(enumOK,"Emilia original appearance3 composes judge1 and sandman2 under verified FlagsAttribute/current assembly hash; related4 separate, not Unity selector execution");
    }

    static void HenriChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/henri_validation_routes.json");Dictionary<string,object> scene=null;foreach(Dictionary<string,object> s in (IEnumerable)reference["scenes"])scene=s;
        var row=(Dictionary<string,object>)rows["075_henri_01"];var seen=new HashSet<string>();var edges=new HashSet<string>();int number=0;
        foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"]) {
            List<string> text,actions;var globals=(Dictionary<string,object>)r["globals"];var story=ReferenceRun(row,r,(string)scene["start"],MathieuGlobals(globals),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,edges);
            var p=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",text.ToArray());bool outNow=p.Contains("henri-01-yourtone-out"),clinic=p.Contains("henri-01-start-open"),memory=p.Contains("henri-01-start-silence")&&Convert.ToInt32(globals["_Driver.Met.Emilia"])==1;bool ok=!story.can_continue&&story.GetChoices().Count==0;
            int sympathy=MathieuValue(story,"sympathy");ok &= sympathy>=-5&&sympathy<=3 && MathieuValue(story,"bet-clinic")== (clinic?1:0) && MathieuValue(story,"_Driver.Met.Emilia")==Convert.ToInt32(globals["_Driver.Met.Emilia"]);
            ok &= !p.Contains("henri-01-end-tip") && p.Contains("henri-01-start-silence-emilia")==memory && HasText(all,"\nКот.\n")==memory;
            ok &= actions.FindAll(a=>a.Contains("travel:")&&a.Contains("start")).Count==1 && actions.FindAll(a=>a.Contains("travel:")&&a.Contains("stop")).Count==1;
            ok &= actions.FindAll(a=>a.Contains("clue:")||a.Contains("destination:")).Count==0 && MathieuValue(story,"_Driver.Met.Henri")==0;
            var fare=actions.FindAll(a=>a.Contains("fare:"));var tips=actions.FindAll(a=>a.Contains("tip:"));
            ok &= fare.Count== (outNow?1:0) && (!outNow||fare[0].Contains("0")) && tips.Count== (outNow?0:1) && (outNow||tips[0].Contains("low"));
            ok &= HasText(all,"угрозы не были пустыми словами")==outNow && HasText(all,"протягивает вам деньги за поездку")==!outNow && HasText(all,"Нафталин")==!outNow;
            ok &= actions.FindAll(a=>a.Contains("075_01")).Count== (outNow?1:0) && actions.FindAll(a=>a.Contains("075_02")).Count==0;
            ok &= HasText(all,"Хожу туда с самого детства")==clinic && HasText(all,"клиника открыта, спасибо")==p.Contains("henri-01-shesgoing-outro");
            bool chanel=p.Contains("henri-01-animals-reincarnation-intro");ok &= HasText(all,"реинкарнацией Коко Шанель")==chanel && HasText(all,"держалась с достоинством")==chanel && !HasText(all,"таким присутствием");
            ok &= HasText(all,"почти ощущаете ногти у себя на голове")==p.Contains("henri-01-silence2") && HasText(all,"Ради неё самой")==p.Contains("henri-01-shesgoing2");
            if(p.Contains("henri-01-silence2"))ok &= text.Contains("Собака коротко рычит — наверное, от боли.");
            if(p.Contains("henri-01-animals-reincarnation-what"))ok &= text.Contains("АНРИ: «Тот же характер, та же чувствительность… Присмотритесь…»");
            if(p.Contains("henri-01-start-silence"))ok &= text.Contains("В конце концов, у него на коленях страдающее животное.");
            Check(ok,"Henri full representative exact native text/menu/state, female dog and staged smell/memory, ordinary sympathy bounds and fare "+(++number));
        }
        Check(number==264&&seen.Count==38&&edges.Count==29,"Henri264full representative routes cover38normally reachable blocks/29answers/114variable menus, high tip unreachable from sympathy0");
        int controlled=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["controls"]) {
            var globals=(Dictionary<string,object>)r["globals"];int value=Convert.ToInt32(globals["sympathy"]);List<string> text,actions;var story=ReferenceRun(PrivateVariables(row,globals),r,(string)r["start"],MathieuGlobals(globals),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,edges);string all=string.Join("\n",text.ToArray());var tips=actions.FindAll(a=>a.Contains("tip:"));controlled++;
            Check(HasText(all,"Сдачу оставьте")== (value>3) && tips.Count== (value>3?2:1) && tips[0].Contains("low") && (value<=3||tips[1].Contains("high")) && actions.FindAll(a=>a.Contains("075_02")).Count== (value>3?1:0) && !story.can_continue,"Henri controlled strict sympathy>3 boundary, exact text/state/actions and unreachable tip/reveal entry initial="+value);
        }
        Check(controlled==2&&seen.Count==39&&edges.Count==29,"Henri ordinary histories plus2explicitcontrols coverall39sourceblocks/29answers; no new incoming high-tip link");
        var previous=ReferenceFile("localization/emilia_validation_routes.json");Dictionary<string,object> emiliaScene=null;foreach(Dictionary<string,object> s in (IEnumerable)previous["scenes"])emiliaScene=s;
        foreach(string choice in new string[]{"emilia-01-cat-clientkills02","emilia-01-cat-driverkills02"}) {
            Dictionary<string,object> before=null,after=null;
            foreach(Dictionary<string,object> r in (IEnumerable)emiliaScene["routes"])if(Strings(r["passages"]).Contains(choice)){before=r;break;}
            foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"])if(Convert.ToInt32(r["mode"])==1 && Strings(r["passages"]).Contains("henri-01-start-silence")){after=r;break;}
            List<string> t,a;var done=ReferenceRun((Dictionary<string,object>)rows["074_emilia_01"],before,(string)emiliaScene["start"],new List<DialogVariable>{new DialogVariable("_Killer.Is.Judge",1)},out t,out a);
            var g=new Dictionary<string,object>((Dictionary<string,object>)after["globals"]);g["_Driver.Met.Emilia"]=MathieuValue(done,"_Driver.Met.Emilia");var next=ReferenceRun(row,after,(string)scene["start"],MathieuGlobals(g),out t,out a);
            Check(MathieuValue(done,"_Driver.Met.Emilia")==1&&HasText(string.Join("\n",t.ToArray()),"\nКот.\n"),"Henri actual completed Emilia route passes Met flag to starting silent cat recollection "+choice);
        }
        var proof=ReferenceFile("review/emilia_killer_flags.json");var values=(Dictionary<string,object>)proof["values"];var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2397.json");bool okEnum=Convert.ToBoolean(proof["flags_attribute"]) && (Convert.ToInt32(values["judge"])|Convert.ToInt32(values["angel"]))==5;
        foreach(Dictionary<string,object> e in (IEnumerable)card["encounters"])okEnum &= Convert.ToInt32(e["appears_only_for_killer"])==5;
        using(var hash=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead((string)proof["assembly"]))okEnum &= BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()==(string)proof["assembly_sha256"];
        Check(okEnum,"Henri source appearance5 is judge1|angel4 using current verified FlagsAttribute/assembly; no Unity selector execution");
        var calls=Strings(row["_user_datas"]);Check(calls.Count==9&&calls[0]=="passengercall: «В шестнадцатый округ, к Молитор!»","Henri French16th/Molitor invitation and8technical animation definitions retained");
    }

    static void AngeliqueActions(Dictionary<string,object> route,List<string> actual) {
        var expected=Strings(route["expected_actions"]);
        if(expected.Count!=actual.Count)throw new Exception("Angelique original action count differs: "+expected.Count+"/"+actual.Count);
        for(int i=0;i<actual.Count;i++)if(expected[i].Trim()!=actual[i].Trim())throw new Exception("Angelique original action order differs: "+expected[i]+" / "+actual[i]);
    }
    static List<string> AngeliqueRandomDrain(PrompterStory story,HashSet<string> boundaries) {
        var text=new List<string>();int n=0;
        while(story.can_continue) {
            if(++n>100)throw new Exception("Angelique random intro loop");
            string line=story.GetNextLine();if(line==null)continue;text.Add(line);
            // GetNextLine can resolve the outgoing link while returning its
            // final visible line. Pace reading throughout this short intro,
            // before both transitions; do not choose outcomes or change Random.
            System.Threading.Thread.Sleep(20);
        }
        return text;
    }
    static void AngeliqueChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/angelique_validation_routes.json");var scenes=new Dictionary<string,Dictionary<string,object>>();
        foreach(Dictionary<string,object> s in (IEnumerable)reference["scenes"])scenes[(string)s["scene"]]=s;
        int total=0;
        foreach(var pair in scenes) {
            string name=pair.Key;var scene=pair.Value;if(name.EndsWith("00b"))continue;
            var seen=new HashSet<string>();var edges=new HashSet<string>();int number=0;
            foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"]) {
                List<string> t,a;var story=ReferenceRun((Dictionary<string,object>)rows[name],r,(string)scene["start"],MathieuGlobals(r["globals"]),out t,out a);AngeliqueActions(r,a);ReferenceCoverage(r,seen,edges);
                var p=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",t.ToArray());bool intro=name.EndsWith("00a");
                bool ok=!story.can_continue&&story.GetChoices().Count==0 && a.FindAll(x=>x.Contains("travel:")&&x.Contains("start")).Count==(intro?1:0) && a.FindAll(x=>x.Contains("travel:")&&x.Contains("stop")).Count==(intro?0:1);
                ok &= a.FindAll(x=>x.Contains("clue:")||x.Contains("destination:")||x.Contains("fare:")).Count==0 && MathieuValue(story,"_Driver.Met.Angelique")==0;
                var tips=a.FindAll(x=>x.Contains("tip:"));bool medium=p.Contains("angel-02-end-bad")||p.Contains("angel-03-end-introC");
                ok &= tips.Count==(intro||medium?1:0) && (tips.Count==0||tips[0].Contains(intro?"high":"medium"));
                if(intro)ok &= HasText(all,"В пятьдесят")==p.Contains("angel-00a-newlife") && HasText(all,"двадцать пять лет") && !HasText(all,"Туретт");
                if(name.EndsWith("01")) {
                    bool how=p.Contains("angel-01-daughter-how");ok &= HasText(all,"открыла счёт в банке") && HasText(all,"ССЫКЛО")==how && HasText(all,"намеренным")==how;
                    ok &= HasText(all,"Теперь я свободна")==!how && HasText(all,"съёмное жильё")==how && HasText(all,"раз пятьдесят") && HasText(all,"новыми, чистыми купюрами");
                    ok &= a.FindAll(x=>x.Contains("076_02")).Count==(how?1:0);
                }
                if(name.EndsWith("02")) {
                    ok &= HasText(all,"делает это незаметно") && !HasText(all,"сам не знает") && HasText(all,"Вы не могли бы подождать")==p.Contains("angel-02-end-ok");
                    ok &= HasText(all,"Жан-Ролан")==p.Contains("angel-02-support") && HasText(all,"Нашего расставания, хотите сказать")==p.Contains("angel-02-divorce") && HasText(all,"ему не принадлежат")==p.Contains("angel-02-perfume-divorce");
                    bool good=p.Contains("angel-02-end-good");ok &= HasText(all,"почти для всех пассажиров")==good && HasText(all,"медные детали лестницы")==good && HasText(all,"проводит рукой по меху")==!good && HasText(all,"Она расплачивается");
                }
                if(name.EndsWith("03")) {
                    ok &= HasText(all,"Жан-Пьер") && HasText(all,"вдвое моложе его") && HasText(all,"психотерапевтом больше сорока часов") && HasText(all,"Может, всё-таки позвонить ей") && HasText(all,"На „т“");
                    ok &= HasText(all,"АНЖЕЛИКА: «Обычно психотерапевт не позволяет") && !HasText(all,"Туретт");
                }
                if(name.EndsWith("04")) {
                    ok &= MathieuValue(story,"_Pierrette.Restaurant.Known")==1 && HasText(all,"„Ла Пьер“") && HasText(all,"деменция") && HasText(all,"маразм") && HasText(all,"Ёбаная дура") && HasText(all,"ХУЕСОСКА");
                    ok &= HasText(all,"Присылает розы")==p.Contains("angel-04-alright") && HasText(all,"Кажется, прошло") && HasText(all,"Лицо вспыхивает");
                }
                Check(ok,"Angelique exact full route, menus/variables/action order and owner/uncertainty constraints "+name+"/"+(++number));total++;
            }
            Check(seen.Count==Convert.ToInt32(scene["passages_covered"])&&edges.Count==Convert.ToInt32(scene["answers_covered"]),"Angelique complete original representative coverage "+name+" passages="+seen.Count+" answers="+edges.Count);
        }
        var random=scenes["076_angelique_00b"];var outcomes=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> r in (IEnumerable)random["random_outcomes"])outcomes.Add(r);
        var boundaries=new HashSet<string>(Strings(random["random_boundaries"]));var observed=new HashSet<int>();int attempts=0;
        while(observed.Count<outcomes.Count && attempts<500) {
            var story=new PrompterStory(Load((Dictionary<string,object>)rows["076_angelique_00b"]),false);var actions=new List<string>();story.OnActionTriggered+=(e)=>actions.Add(e.action);story.StartDialog(null,(string)random["start"]);
            var text=AngeliqueRandomDrain(story,boundaries);int match=-1;
            for(int i=0;i<outcomes.Count;i++){var wanted=Strings(outcomes[i]["expected_texts"]);if(text.Count!=wanted.Count)continue;bool same=true;for(int j=0;j<text.Count;j++)same &= text[j]==wanted[j];if(same){match=i;break;}}
            if(match<0)throw new Exception("Angelique repeat intro outside nine independent original outcomes");AngeliqueActions(outcomes[match],actions);observed.Add(match);
            bool ok=!story.can_continue&&story.GetChoices().Count==0 && actions.FindAll(x=>x.Contains("tip:")&&x.Contains("high")).Count==1 && actions.FindAll(x=>x.Contains("travel:")&&x.Contains("start")).Count==1 && actions.FindAll(x=>x.Contains("travel:")&&x.Contains("stop")).Count==0;
            Check(ok,"Angelique actual random complete intro "+(++attempts)+" outcome="+match);System.Threading.Thread.Sleep(20);
        }
        Check(observed.Count==9 && total==51,"Angelique all9 actual independent RNG outcomes and51deterministic routes cover81passages/61answers; reading paced without seed/link changes");
        Dictionary<string,object> introRoute=null;foreach(Dictionary<string,object> r in (IEnumerable)scenes["076_angelique_00a"]["routes"]){introRoute=r;break;}
        foreach(string suffix in new string[]{"01","02","03","04"}) {
            var mainScene=scenes["076_angelique_"+suffix];Dictionary<string,object> selected=null;
            foreach(Dictionary<string,object> r in (IEnumerable)mainScene["routes"])if(selected==null||(suffix=="02"&&Strings(r["passages"]).Contains("angel-02-end-bad"))||(suffix=="03"&&Strings(r["passages"]).Contains("angel-03-end-introC")))selected=r;
            List<string> t,a;ReferenceRun((Dictionary<string,object>)rows["076_angelique_00a"],introRoute,(string)scenes["076_angelique_00a"]["start"],null,out t,out a);var commands=new List<string>(a);
            ReferenceRun((Dictionary<string,object>)rows["076_angelique_"+suffix],selected,(string)mainScene["start"],MathieuGlobals(selected["globals"]),out t,out a);commands.AddRange(a);var tips=commands.FindAll(x=>x.Contains("tip:"));
            Check(commands.FindAll(x=>x.Contains("travel:")&&x.Contains("start")).Count==1 && commands.FindAll(x=>x.Contains("travel:")&&x.Contains("stop")).Count==1 && tips[0].Contains("high") && tips[tips.Count-1].Contains(suffix=="02"||suffix=="03"?"medium":"high"),"Angelique paired actual intro/story segments preserve travel and ordered tip inheritance "+suffix+"; not Unity orchestration execution");
        }
        var pierrette=ReferenceFile("localization/marc_pierrette_validation_routes.json");Dictionary<string,object> chefScene=null,chefRoute=null;
        foreach(Dictionary<string,object> s in (IEnumerable)pierrette["scenes"])if((string)s["scene"]=="072_pierrette_01")chefScene=s;
        foreach(Dictionary<string,object> r in (IEnumerable)chefScene["routes"])if(Convert.ToInt32(r["mode"])==1){chefRoute=r;break;}
        for(int known=0;known<2;known++) {
            Dictionary<string,object> before=null;foreach(Dictionary<string,object> r in (IEnumerable)scenes["076_angelique_04"]["routes"])if(Convert.ToInt32(r["mode"])==known){before=r;break;}
            List<string> t,a;var done=ReferenceRun((Dictionary<string,object>)rows["076_angelique_04"],before,(string)scenes["076_angelique_04"]["start"],MathieuGlobals(before["globals"]),out t,out a);
            var g=new Dictionary<string,object>((Dictionary<string,object>)chefRoute["globals"]);g["_Pierrette.Restaurant.Known"]=MathieuValue(done,"_Pierrette.Restaurant.Known");ReferenceRun((Dictionary<string,object>)rows["072_pierrette_01"],chefRoute,(string)chefScene["start"],MathieuGlobals(g),out t,out a);
            Check(MathieuValue(done,"_Pierrette.Restaurant.Known")==1&&HasText(string.Join("\n",t.ToArray()),"Слышал о нём")&&!HasText(string.Join("\n",t.ToArray()),"Не знаю такого"),"Angelique actual completed dinner story passes restaurant knowledge to Pierrette "+known);
        }
    }
    static Dialog SantaSleepCompile(string file) {
        var assembly=Assembly.LoadFrom("review/PrompterCompiler.dll");var parser=assembly.GetType("PrompterCompiler.Parser.PrompterParser");
        var method=parser.GetMethod("Compile",new Type[]{typeof(string),typeof(bool)});
        var result=(Dialog)method.Invoke(method.IsStatic?null:Activator.CreateInstance(parser),new object[]{Path.GetFullPath(file),false});
        if(result==null)throw new Exception("Santa linked sleep compiler returned null");return result;
    }
    static void SantaChecks(Dictionary<string,object> row) {
        var reference=ReferenceFile("localization/santa_validation_routes.json");var seen=new HashSet<string>();var edges=new HashSet<string>();int number=0,policies=0,met=0;
        foreach(Dictionary<string,object> policy in (IEnumerable)reference["policies"]) {
            var candidates=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> r in (IEnumerable)policy["candidates"])candidates.Add(r);
            var observed=new HashSet<string>();int attempts=0;
            while(observed.Count<candidates.Count&&attempts++<80) {
                Dictionary<string,object> matched;List<string> text,actions;var story=MyriamRandomRun(row,policy,null,out matched,out text,out actions);AngeliqueActions(matched,actions);ReferenceCoverage(matched,seen,edges);
                var p=new HashSet<string>(Strings(matched["passages"]));bool flying=p.Contains("santa-01-end-sledge"),waiting=p.Contains("santa-01-end-sledgeno"),how=p.Contains("santa-01-driver2-how"),quiet=p.Contains("santa-01-kids-silence");observed.Add(flying?"flying":waiting?"wait":"ordinary");string all=string.Join("\n",text.ToArray());
                bool ok=!story.can_continue&&story.GetChoices().Count==0&&MathieuValue(story,"_Driver.Met.Santa")==1;
                ok &= actions.FindAll(a=>a.Contains("travel:")&&a.Contains("start")).Count==2 && actions.FindAll(a=>a.Contains("travel:")&&a.Contains("stop")).Count==1;
                var tips=actions.FindAll(a=>a.Contains("tip:"));ok &= tips.Count==1&&tips[0].Contains("medium") && actions.FindAll(a=>a.Contains("fare:")||a.Contains("clue:")||a.Contains("destination:")).Count==0;
                ok &= HasText(all,"Рот прополоскать") && !HasText(all,"палитр") && HasText(all,"мобильники в восемь лет") && HasText(all,"Сани да две полудюжины оленей") && HasText(all,"Он расплачивается") && HasText(all,"Рождество уже прошло");
                ok &= HasText(all,"автоматы и куклы")==!quiet && HasText(all,"слоем в метр")==quiet;
                ok &= HasText(all,"под названием… тюрьма")==how && HasText(all,"Вы чувствуете огромное облегчение")==!how && HasText(all,"Брат тебе жизни не давал") && HasText(all,"Будто удар в живот");
                ok &= HasText(all,"Силуэт тут же исчезает")==flying && HasText(all,"у тебя с головой неладно")==flying && HasText(all,"Проходит мгновение")==waiting;
                ok &= actions.FindAll(a=>a.Contains("077_03")).Count==(flying?1:0) && actions.FindAll(a=>a.Contains("077_04")).Count==(waiting?1:0) && !HasText(all,"Дед Мороз");
                Check(ok,"Santa actual complete policy preserves original menus/text/actions, metaphors and paid ambiguous ending "+policies+"/"+(++number)+" "+(flying?"flying":waiting?"wait":"ordinary"));met=MathieuValue(story,"_Driver.Met.Santa");System.Threading.Thread.Sleep(20);
            }
            Check(observed.Count==candidates.Count,"Santa all permitted actual endings for full representative policy "+policies+" outcomes="+observed.Count);policies++;
        }
        Check(policies==13 && seen.Count==32 && edges.Count==19,"Santa13complete policies cover32original passages/19answers with25permitted outcomes; not every Cartesian prefix");
        var sleep=SantaSleepCompile((string)reference["sleep_script"]);var fragment=(Dictionary<string,object>)reference["sleep_fragment"];var wanted=Strings(fragment["texts"]);
        for(int lower=0;lower<4;lower++) {
            var remembered=new PrompterStory(sleep,false);var actions=new List<string>();remembered.OnActionTriggered+=(e)=>actions.Add(e.action);
            remembered.StartDialog(new List<DialogVariable>{new DialogVariable("_Day.Number",5),new DialogVariable("_Driver.Met.Santa",met),new DialogVariable("_Driver.Met.Houssine",lower&1),new DialogVariable("_Driver.Met.Alexandre",(lower>>1)&1)},(string)reference["sleep_start"]);
            var actual=new List<string>();int guard=0;while(remembered.can_continue&&actual.Count<wanted.Count){if(++guard>40)throw new Exception("Santa sleep fragment loop");string line=remembered.GetNextLine();if(line!=null)actual.Add(line);}
            bool same=met==1&&actual.Count==wanted.Count;for(int i=0;i<actual.Count&&same;i++)same &= actual[i]==wanted[i];
            Check(same&&actions.Exists(a=>a.Contains("077_01"))&&HasText(string.Join("\n",actual.ToArray()),"Это и правда был Санта-Клаус?"),"Santa actual completed ride flag selects compiled930day5 questioning memory before lower-priority clients "+lower+"; fragment only");
        }
        var absent=new PrompterStory(sleep,false);var absentActions=new List<string>();absent.OnActionTriggered+=(e)=>absentActions.Add(e.action);absent.StartDialog(new List<DialogVariable>{new DialogVariable("_Day.Number",5),new DialogVariable("_Driver.Met.Santa",0),new DialogVariable("_Driver.Met.Houssine",0),new DialogVariable("_Driver.Met.Alexandre",0)},(string)reference["sleep_start"]);var other=Drain(absent);
        Check(!absentActions.Exists(a=>a.Contains("077_01"))&&!HasText(string.Join("\n",other.ToArray()),"Это и правда был Санта-Клаус?"),"Santa missing encounter flag skips linked930memory; one actual fallback tail only");
    }
    static PrompterStory HoussineRandomRun(Dictionary<string,object> row,Dictionary<string,object> scene,int met,int replay,out Dictionary<string,object> matched,out List<string> text,out List<string> actions) {
        var story=new PrompterStory(Load(row),false);var emitted=new List<string>();actions=emitted;
        story.OnActionTriggered+=(e)=>{
            emitted.Add(e.action);
            // Time-seeded RNG links can be resolved while returning a line.
            // Pace visible reading and the actual reveal/animation boundary
            // between RNG groups; never change game links, seeds or outcomes.
            if(e.action.Trim()=="music-control: 20"||e.action.Trim()=="anim: IDLE_ANGRY")System.Threading.Thread.Sleep(20+(replay%13));
        };
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Houssine",met)},(string)scene["start"]);text=new List<string>();int n=0;
        while(story.can_continue) {
            if(++n>100)throw new Exception("Houssine random intro loop");string line=story.GetNextLine();if(line==null)continue;text.Add(line);System.Threading.Thread.Sleep(12+((replay*7+n*5)%19));
        }
        matched=null;foreach(Dictionary<string,object> r in (IEnumerable)scene["random_outcomes"]) {
            var wanted=Strings(r["expected_texts"]);bool same=text.Count==wanted.Count;if(same)for(int i=0;i<text.Count;i++)same &= text[i]==wanted[i];if(same){matched=r;break;}
        }
        if(matched==null)throw new Exception("Houssine random text outside150independent original combinations");AngeliqueActions(matched,actions);return story;
    }
    static void HoussineChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/houssine_validation_routes.json");var scenes=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> s in (IEnumerable)reference["scenes"])scenes[(string)s["scene"]]=s;
        int total=0,metFromFirst=0;
        foreach(var pair in scenes) {
            string name=pair.Key;var scene=pair.Value;if(name.EndsWith("00b"))continue;var seen=new HashSet<string>();var edges=new HashSet<string>();int number=0;
            foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"]) {
                List<string> text,actions;var globals=(Dictionary<string,object>)r["globals"];var story=ReferenceRun((Dictionary<string,object>)rows[name],r,(string)scene["start"],MathieuGlobals(globals),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,edges);
                var p=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",text.ToArray());bool first=name.EndsWith("00a");int met=Convert.ToInt32(globals["_Driver.Met.Houssine"]);
                bool ok=!story.can_continue&&story.GetChoices().Count==0&&MathieuValue(story,"_Driver.Met.Houssine")== (first?1:met);
                ok &= actions.FindAll(a=>a.Contains("travel:")||a.Contains("fare:")||a.Contains("destination:")).Count==0 && !HasText(all,"Хуссин");
                var tips=actions.FindAll(a=>a.Contains("tip:"));ok &= tips.Count==(first?1:0)&&(tips.Count==0||tips[0].Contains("none"));
                string clue=name.EndsWith("01")?"clue_angel_houssine_01":name.EndsWith("02")?"clue_sand_houssine_01":name.EndsWith("03")?"clue_judge_houssine_01":null;
                var clues=actions.FindAll(a=>a.Contains("clue:"));ok &= clues.Count==(clue==null?0:1)&&(clue==null||clues[0].Contains(clue));
                if(first){ok &= HasText(all,"видите себя на заднем сиденье")&&HasText(all,"Тебе не нужно спать");metFromFirst=MathieuValue(story,"_Driver.Met.Houssine");}
                if(name.EndsWith("01"))ok &= HasText(all,"Ангел смерти")&&HasText(all,"Убийца… был невысоким")&&HasText(all,"женоподобный")&&HasText(all,"бент кахба")&&!HasText(all,"мигрень");
                if(name.EndsWith("02")) {
                    ok &= HasText(all,"Песочный человек")&&HasText(all,"Он сказал… „Прости“")&&HasText(all,"Считаете до тридцати");
                    ok &= HasText(all,"В каком-то смысле — твоего брата")==p.Contains("houssine-02-start-mytaxi")&&HasText(all,"Ему там, где он есть, очень даже хорошо")==p.Contains("houssine-02-start-mytaxi-silence");
                }
                if(name.EndsWith("03")) {
                    bool no=p.Contains("houssine-03-remember1-no"),ade=no&&Convert.ToInt32(globals["_Driver.Met.Ade"])==1;
                    ok &= HasText(all,"вы слышите голос Аде")==ade && HasText(all,"Женский… Голос подруги")== (no&&!ade) && HasText(all,"родной")==no;
                    ok &= HasText(all,"прикинуться дураком")==p.Contains("houssine-03-remember0-intro")&&!HasText(all,"моя дорогая")&&!HasText(all,"дурочкой")&&HasText(all,"Морщины на коже")&&HasText(all,"С тальком")&&HasText(all,"Вы задремали");
                }
                if(name.EndsWith("04"))ok &= HasText(all,"когда умер папа")&&HasText(all,"маму видел")&&HasText(all,"пятнадцать лет")==p.Contains("houssine-04-story3-intro")&&HasText(all,"На его месте")&&HasText(all,"всего лишь кот");
                if(name.EndsWith("05")) {
                    ok &= HasText(all,"У меня не было выбора")==p.Contains("houssine-05-notawhore")&&!HasText(all,"покончил с собой")&&HasText(all,"Брата-то ты всё-таки убил")&&HasText(all,"три месяца в университете")&&HasText(all,"по всему Парижу");
                    ok &= HasText(all,"Люблю, когда ты злишься")==p.Contains("houssine-05-getout2-intro");
                }
                if(name.EndsWith("06")) {
                    ok &= HasText(all,"Синтия")&&HasText(all,"мочеприёмник")&&HasText(all,"Жюли")&&!HasText(all,"Джу-ли")&&HasText(all,"Пассажирка. Улыбка");
                    ok &= HasText(all,"Она меня пугала")==p.Contains("houssine-06-truth")&&HasText(all,"Она мне не нравилась")==p.Contains("houssine-06-lie")&&HasText(all,"Трахнуть Жюли")==p.Contains("houssine-06-leaveme");
                    var speed=actions.FindAll(a=>a.Contains("dialogspeed:"));ok &= speed.Count==1&&speed[0].Contains("slow")&&HasText(all,"Металл тёплый, успокаивающий");
                }
                Check(ok,"Houssine exact complete representative route preserves unnamed image/case/voice/family context and source anomalies "+name+"/"+(++number));total++;
            }
            Check(seen.Count==Convert.ToInt32(scene["passages_covered"])&&edges.Count==Convert.ToInt32(scene["answers_covered"]),"Houssine complete original menu coverage "+name+" passages="+seen.Count+" answers="+edges.Count);
        }
        var repeat=scenes["090_houssine_00b"];var combinations=new HashSet<string>();var repeatPassages=new HashSet<string>();int attempts=0;
        // Bounded sampling verifies unchanged runtime selection. Complete text
        // coverage below uses all source-permitted private control paths.
        // Waiting for a time-seeded coupon collector is not a coverage proof.
        while(attempts<32) {
            Dictionary<string,object> matched;List<string> text,actions;var story=HoussineRandomRun((Dictionary<string,object>)rows["090_houssine_00b"],repeat,metFromFirst,attempts,out matched,out text,out actions);
            var path=Strings(matched["passages"]);string key=string.Join("|",path.ToArray());combinations.Add(key);foreach(string title in path)repeatPassages.Add(title);
            bool ok=metFromFirst==1&&MathieuValue(story,"_Driver.Met.Houssine")==1&&!story.can_continue&&story.GetChoices().Count==0&&actions.FindAll(a=>a.Contains("tip:")&&a.Contains("none")).Count==1&&actions.FindAll(a=>a.Contains("travel:")||a.Contains("clue:")||a.Contains("fare:")||a.Contains("destination:")).Count==0;
            Check(ok,"Houssine actual RNG complete repeated intro "+(++attempts)+" observed="+combinations.Count+"/150");System.Threading.Thread.Sleep(20);
        }
        var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=50000000;
        var originalRepeat=(Dictionary<string,object>)rows["090_houssine_00b"];string untouched=serializer.Serialize(originalRepeat);int controls=0;
        foreach(Dictionary<string,object> outcome in (IEnumerable)repeat["random_outcomes"]) {
            var selected=Strings(outcome["passages"]);var forced=new Dictionary<string,string>();
            for(int i=0;i+1<selected.Count;i++)forced[selected[i]]=selected[i+1];
            var copy=new Dictionary<string,object>(originalRepeat);var privatePassages=new List<object>();
            foreach(Dictionary<string,object> p in (IEnumerable)originalRepeat["_passages"]) {
                var privatePassage=new Dictionary<string,object>(p);string title=(string)p["_title"],link=(string)p["_link"];
                if(link.StartsWith("??")&&forced.ContainsKey(title)) {
                    if(Array.IndexOf(link.Substring(2).Split('|'),forced[title])<0)throw new Exception("Houssine controlled edge outside original RNGlist");
                    privatePassage["_link"]=forced[title];
                }
                privatePassages.Add(privatePassage);
            }
            copy["_passages"]=privatePassages;
            var route=new Dictionary<string,object>(outcome);route["answers"]=new object[0];route["edges"]=new object[0];
            List<string> text,actions;var story=ReferenceRun(copy,route,(string)repeat["start"],new List<DialogVariable>{new DialogVariable("_Driver.Met.Houssine",metFromFirst)},out text,out actions);AngeliqueActions(route,actions);
            foreach(string title in selected)repeatPassages.Add(title);
            Check(metFromFirst==1&&MathieuValue(story,"_Driver.Met.Houssine")==1&&!story.can_continue&&story.GetChoices().Count==0,"Houssine controlled allowed combination "+(++controls)+" private source-edge copy; exact text/state/actions, not actual random selection");
        }
        Check(total==88&&attempts==32&&controls==150&&repeatPassages.Count==21&&serializer.Serialize(originalRepeat)==untouched,"Houssine all150controlled independent5x3x10outcomes plus88deterministic routes cover91passages/47answers;32unchanged actualRNG intros observed="+combinations.Count+"/150;originalrow unchanged");
        Dictionary<string,object> freshMatched;List<string> freshText,freshActions;var fresh=HoussineRandomRun((Dictionary<string,object>)rows["090_houssine_00b"],repeat,0,attempts,out freshMatched,out freshText,out freshActions);
        Check(MathieuValue(fresh,"_Driver.Met.Houssine")==0,"Houssine controlled repeat-intro start with missing first meeting does not invent Met flag");
        foreach(string suffix in new string[]{"01","02","03","04","05","06"}) {
            var s=scenes["090_houssine_"+suffix];Dictionary<string,object> r=null;foreach(Dictionary<string,object> candidate in (IEnumerable)s["routes"])if(Convert.ToInt32(((Dictionary<string,object>)candidate["globals"])["_Driver.Met.Houssine"])==1){r=candidate;break;}
            var g=new Dictionary<string,object>((Dictionary<string,object>)r["globals"]);g["_Driver.Met.Houssine"]=metFromFirst;List<string> t,a;var next=ReferenceRun((Dictionary<string,object>)rows["090_houssine_"+suffix],r,(string)s["start"],MathieuGlobals(g),out t,out a);
            Check(MathieuValue(next,"_Driver.Met.Houssine")==1,"Houssine actual first intro flag retained by complete case/family segment "+suffix+"; pairedsegments not Unity orchestration");
        }
        var sleep=SantaSleepCompile((string)reference["sleep_script"]);var fragments=(Dictionary<string,object>)reference["sleep_fragments"];
        for(int santa=0;santa<2;santa++)for(int alexandre=0;alexandre<2;alexandre++) {
            string title=santa==1?"sleep-client-077santa":"sleep-client-090houssine";var fragment=(Dictionary<string,object>)fragments[title];var wanted=Strings(fragment["texts"]);var story=new PrompterStory(sleep,false);var actions=new List<string>();story.OnActionTriggered+=(e)=>actions.Add(e.action);
            story.StartDialog(new List<DialogVariable>{new DialogVariable("_Day.Number",5),new DialogVariable("_Driver.Met.Santa",santa),new DialogVariable("_Driver.Met.Houssine",metFromFirst),new DialogVariable("_Driver.Met.Alexandre",alexandre)},(string)reference["sleep_start"]);var t=new List<string>();int guard=0;while(story.can_continue&&t.Count<wanted.Count){if(++guard>40)throw new Exception("Houssine sleep fragment loop");string line=story.GetNextLine();if(line!=null)t.Add(line);}
            bool ok=t.Count==wanted.Count;for(int i=0;i<t.Count&&ok;i++)ok &= t[i]==wanted[i];
            ok &= actions.Exists(a=>a.Contains("090_01"))==(santa==0)&&actions.Exists(a=>a.Contains("077_01"))==(santa==1);
            Check(ok,"Houssine actual obtainedMet selects compiled930self-face memory under Santa/Houssine/Alexandre priority "+santa+"/"+alexandre+"; fragment only");
        }
        var absent=new PrompterStory(sleep,false);var absentActions=new List<string>();absent.OnActionTriggered+=(e)=>absentActions.Add(e.action);absent.StartDialog(new List<DialogVariable>{new DialogVariable("_Day.Number",5),new DialogVariable("_Driver.Met.Santa",0),new DialogVariable("_Driver.Met.Houssine",0),new DialogVariable("_Driver.Met.Alexandre",0)},(string)reference["sleep_start"]);Drain(absent);
        Check(!absentActions.Exists(a=>a.Contains("090_01")),"Houssine missing meeting skips compiled930self-image memory; one fallback only");
        var checkedUI=ReferenceFile("localization/contexts/090_houssine_ui.json");var ui=ReferenceFile("../translation/ui_ru.json");var mapping=ReferenceFile("revision/data/Russian_UI/full_translation_mapping.json");int keys=0;
        foreach(Dictionary<string,object> k in (IEnumerable)checkedUI["keys"]){if((string)ui[(string)k["key"]]!=(string)k["russian"]||(string)mapping[(string)k["english"]]!=(string)k["russian"])throw new Exception("Houssine reviewed clue UI mismatch");keys++;}
        Check(keys==6&&((string)ui["clue_angel_houssine_01.title"]).Contains("1,70")&&((string)ui["clue_sand_houssine_01.title"]).Contains("извинился")&&((string)ui["clue_judge_houssine_01.source"]).Contains("сне?"),"Houssine6case-specific reviewed UI keys preserve height threshold/apology/wrinkledhands and questioned dream source");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2400.json");var enumProof=ReferenceFile("review/emilia_killer_flags.json");var values=(Dictionary<string,object>)enumProof["values"];bool cardOK=(string)card["fullname"]=="???"&&Convert.ToInt32(card["age"])==32&&Convert.ToInt32(card["gender"])==3&&Convert.ToInt32(card["related_to_killers"])==-1&&Convert.ToBoolean(enumProof["flags_attribute"]);int index=0;
        foreach(Dictionary<string,object> e in (IEnumerable)card["encounters"]){int expected=index==0?Convert.ToInt32(values["angel"]):index==1?Convert.ToInt32(values["sandman"]):index==2?Convert.ToInt32(values["judge"]):0;cardOK &= Convert.ToInt32(e["appears_only_for_killer"])==expected&&Convert.ToInt32(e["has_clues_for_killer"])==expected;index++;}
        using(var hash=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead((string)enumProof["assembly"]))cardOK &= BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()==(string)enumProof["assembly_sha256"];
        Check(cardOK&&index==6,"Houssine unknown-name/male32sourcecard, Angel4/Sandman2/Judge1slots and0family/-1related using currentenumassembly proof; no Unity selector claim");
    }
    static int MathieuValue(PrompterStory story,string key) {var v=story.FindVariable(key);return v==null?0:v.value;}
    static List<DialogVariable> MathieuGlobals(object data) {
        var result=new List<DialogVariable>();foreach(var p in (Dictionary<string,object>)data)result.Add(new DialogVariable(p.Key,Convert.ToInt32(p.Value)));return result;
    }
    static PrompterStory MathieuSilenceStart(Dictionary<string,object> row,string passage,int value) {
        var d=Load(row);foreach(var v in (List<DialogVariable>)d.GetType().GetField("_variables",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(d))if(v.name=="silence")Set(v,"_value",value);
        return Start(d,passage);
    }
    static void MathieuChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/mathieu_fr_validation_routes.json");int total=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];var row=(Dictionary<string,object>)rows[name];var seen=new HashSet<string>();var edges=new HashSet<string>();int number=0;
            foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"]) {
                List<string> text,actions;var story=ReferenceRun(row,r,(string)scene["start"],MathieuGlobals(r["globals"]),out text,out actions);
                ReferenceCoverage(r,seen,edges);var p=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",text.ToArray());bool ok=true;int episode=Int32.Parse(name.Substring(name.Length-2));
                ok &= actions.FindAll(a=>a.Contains("travel: start")).Count==1 && actions.FindAll(a=>a.Contains("travel: stop")).Count==1;
                ok &= actions.FindAll(a=>a.Contains("clue:") || a.Contains("destination:") || a.Contains("fare:")).Count==0;
                var tips=actions.FindAll(a=>a.Contains("tip:"));
                if(episode==1) {
                    bool talked=p.Contains("mathieu-01-drive-talk"),pity=p.Contains("mathieu-01-ileft-sorry");
                    ok &= MathieuValue(story,"_Mathieu.Talked.Father")== (talked?1:0) && MathieuValue(story,"_Mathieu.Talked.Talked.Father")==0;
                    ok &= HasText(all,"Гинеколог")==talked && HasText(all,"метастазы")==talked && HasText(all,"Всё это правда")==p.Contains("mathieu-01-hospital-pneumonia");
                    ok &= HasText(all,"не закрыв за собой дверь") && tips.Count==(pity?2:1) && tips[0].Contains("low") && (!pity||tips[1].Contains("high"));
                    string[] keys={"CarAccident","Fall","Pneumonia","Vague"};string[] blocks={"car accident","fall","pneumonia","nothing"};
                    for(int i=0;i<keys.Length;i++)ok &= MathieuValue(story,"_Mathieu_Hospital_"+keys[i])==(p.Contains("mathieu-01-hospital-"+blocks[i])?1:0);
                } else if(episode==2) {
                    int mode=Convert.ToInt32(r["mode"]);bool fatherExpected=Convert.ToInt32(((Dictionary<string,object>)r["globals"])["_Mathieu.Talked.Father"])==1,bar=p.Contains("mathieu-02-drink"),home=p.Contains("mathieu-02-drink-no-end"),early=p.Contains("mathieu-02-end-tears");
                    ok &= p.Contains("mathieu-02-father")==fatherExpected && HasText(all,"мой отец умер сегодня под вечер") == fatherExpected;
                    ok &= fatherExpected?HasText(all,"тридцать пять лет"):!HasText(all,"Семьдесят два года");
                    ok &= (bar?1:0)+(home?1:0)+(early?1:0)==1 && tips[0].Contains("none") && tips.Count==(early?1:2);
                    ok &= early || tips[1].Contains(bar?"high":"low");
                    ok &= HasText(all,"поясом взрывчатки")==p.Contains("mathieu-02-end-tears-excuse");
                    ok &= !HasText(all,"люльк") && !HasText(all,"Селест") && !HasText(all,"Матильд");
                } else if(episode==3) {
                    int silence=(p.Contains("mathieu-03-kids-intro")?1:-1)+(p.Contains("mathieu-03-kids-silence")?1:0)+(p.Contains("mathieu-03-tick-intro")?1:0)+(p.Contains("mathieu-03-tick-silence")?2:0)+(p.Contains("mathieu-03-father-silence")?1:0)-(p.Contains("mathieu-03-wakeup-calm")||p.Contains("mathieu-03-wakeup-name")?1:0)+1;
                    bool death=p.Contains("mathieu-03-death-intro"),babyName=p.Contains("mathieu-03-wakeup-name")||death,empty=p.Contains("mathieu-03-end-retro");
                    ok &= MathieuValue(story,"silence")==silence && MathieuValue(story,"mathilde_dead")== (death?1:0);
                    ok &= MathieuValue(story,"_Mathieu.Wife.Name")== (death?1:0) && MathieuValue(story,"_Mathieu.Wife.Dead")== (death?1:0) && MathieuValue(story,"_Mathieu.Baby.Name")== (babyName?1:0) && MathieuValue(story,"_Mathieu.Empty")== (empty?1:0);
                    ok &= HasText(all,"не слышно даже дыхания ребёнка") && HasText(all,"поклясться, что она пуста")==empty && HasText(all,"её мамы")==p.Contains("mathieu-03-death-mathilde");
                    ok &= HasText(all,"статистическая аномалия")==death && HasText(all,"лично для меня статистика")==p.Contains("mathieu-03-kids-truth-notoften");
                    ok &= !HasText(all,"пластиковая кукла") && !HasText(all,"перевозчик") && !HasText(all,"носител");
                    bool tickYes=p.Contains("mathieu-03-tick-yes");ok &= tips.Count==(tickYes?2:1) && tips[0].Contains("medium") && (!tickYes||tips[1].Contains("high"));
                } else {
                    var g=(Dictionary<string,object>)r["globals"];bool oldEmpty=Convert.ToInt32(g["_Mathieu.Empty"])==1,oldBaby=Convert.ToInt32(g["_Mathieu.Baby.Name"])==1,oldWife=Convert.ToInt32(g["_Mathieu.Wife.Name"])==1,oldDeath=Convert.ToInt32(g["_Mathieu.Wife.Dead"])==1;
                    bool early=p.Contains("mathieu-04-emptycoffin-end"),mask=p.Contains("mathieu-04-mask"),reveal=p.Contains("mathieu-04-mask-reveal"),eyes=p.Contains("mathieu-04-mask2-baby");
                    bool greeting=p.Contains("mathieu-04-start-hub-name")||p.Contains("mathieu-04-start-hub");
                    ok &= p.Contains("mathieu-04-start-empty")==oldEmpty && (!greeting||p.Contains("mathieu-04-start-hub-name")==oldBaby);
                    ok &= early || p.Contains("mathieu-04-story-tears-outroA")==oldDeath;
                    ok &= HasText(all,"В люльке лишь пластиковая кукла")==reveal && HasText(all,"Люльку с куклой вы убрали в багажник")==reveal && HasText(all,"Они показались")==eyes;
                    ok &= MathieuValue(story,"wife-talked")== (p.Contains("mathieu-04-yourwifeisdead")?1:0) && p.Contains("mathieu-04-emptycoffin2-outroA")== (early && p.Contains("mathieu-04-yourwifeisdead"));
                    ok &= !p.Contains("mathieu-04-philo-sadness-wife") || oldWife;
                    ok &= !p.Contains("mathieu-04-stress-silence") || p.Contains("mathieu-04-philo")== (MathieuValue(story,"silence")<=5);
                    ok &= tips[0].Contains("medium") && tips.Count==(early||mask?2:1) && (!(early||mask)||tips[1].Contains(early?"none":"low"));
                    ok &= !HasText(all,"перевозчик") && !HasText(all,"носител") && !HasText(all,"гроб");
                }
                Check(ok,"Mathieu complete representative native route exact text/menus/state, family uncertainty, carrier, thresholds and tips "+name+"/"+(++number));total++;
            }
            Check(seen.Count==Convert.ToInt32(scene["passages_covered"]) && edges.Count==Convert.ToInt32(scene["answers_covered"]),"Mathieu coverage of all listed input-state menus/answers "+name+" routes="+number+" passages="+seen.Count+" answers="+edges.Count);
        }
        Check(total==1587,"Mathieu1587complete representative routes: original nine02profiles with approved FRgate/rib, unchanged01/03/04signatures, not Cartesian combinations or liveUnity");
        int compatibleCount=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["legacy_compatible_02_routes"]) {
            List<string> text,actions;
            var actual=ReferenceRun((Dictionary<string,object>)rows["067_mathieu_02"],r,"mathieu-02-start",MathieuGlobals(r["globals"]),out text,out actions);
            Check(!actual.can_continue && actual.GetChoices().Count==0,"Mathieu retained source-compatible old02full path with only original silence index2to3 remap "+(++compatibleCount));
        }
        Check(compatibleCount==94,"Mathieu all94source-compatible old02routes preserved;24old quiet routes with Talked1 superseded by originalFR, all9input profiles remain");
        foreach(string target in new string[]{"mathieu-03-tears-youok","mathieu-03-tears-silence"}) {
            int threshold=target.EndsWith("youok")?3:2;
            foreach(int value in new int[]{threshold,threshold+1}) {
                var s=MathieuSilenceStart((Dictionary<string,object>)rows["067_mathieu_03"],target,value);string text=string.Join("\n",Drain(s).ToArray());
                Check(MathieuValue(s,"mathilde_dead")== (value>threshold?0:1) && HasText(text,"У нас получится — без неё")== (value<=threshold),"Mathieu03strictsilence boundary controlled "+target+" value="+value);
            }
        }
        foreach(int value in new int[]{4,5}) {
            var s=MathieuSilenceStart((Dictionary<string,object>)rows["067_mathieu_04"],"mathieu-04-stress-silence",value);string text=string.Join("\n",Drain(s).ToArray());
            Check(MathieuValue(s,"silence")==value+1 && HasText(text,"Родители не всемогущи")== (value==4),"Mathieu04strict >5 after source+1 controlled initial="+value);
        }
        {
            var row=(Dictionary<string,object>)rows["067_mathieu_02"];PrompterStory story;
            // Original FR rib answer now reaches this paragraph; retain the direct prior-fall probe too.
            story=new PrompterStory(Load(row),false);story.OnActionTriggered+=(a)=>{};story.StartDialog(new List<DialogVariable>{new DialogVariable("_Mathieu_Hospital_Fall",1)},"mathieu-02-lie-fall-hub");
            string text=string.Join("\n",Drain(story).ToArray());Check(HasText(text,"Теперь вспомнил") && !HasText(text,"сломанное ребро"),"Mathieu controlled direct priorfall probe retained in addition to reachable original French rib answer");
        }
        {
            var row=(Dictionary<string,object>)rows["067_mathieu_04"];var story=Start(Load(row),"mathieu-04-story-er-introC");string text=string.Join("\n",Drain(story).ToArray());
            Check(HasText(text,"Пусть клиника и частная, всё равно непросто") && MathieuValue(story,"silence")==1,"Mathieu disconnectedERintroC direct entry preserves original-=1/+2counter");
        }
        // Real completed01 passes its actual singleTalked flag into02.
        // The obsolete EN typo remains0; the approved original FR gate uses the real flag.
        var scenes=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> s in (IEnumerable)reference["scenes"])scenes.Add(s);
        foreach(bool talked in new bool[]{false,true}) {
            Dictionary<string,object> chosen=null;foreach(Dictionary<string,object> r in (IEnumerable)scenes[0]["routes"])if(Strings(r["passages"]).Contains("mathieu-01-drive-talk")==talked){chosen=r;break;}
            List<string> text,actions;var first=ReferenceRun((Dictionary<string,object>)rows["067_mathieu_01"],chosen,"mathieu-01-start",null,out text,out actions);
            var flags=new List<DialogVariable>();foreach(string k in new string[]{"_Mathieu.Talked.Father","_Mathieu.Talked.Talked.Father","_Mathieu_Hospital_CarAccident","_Mathieu_Hospital_Fall","_Mathieu_Hospital_Pneumonia","_Mathieu_Hospital_Vague"})flags.Add(new DialogVariable(k,MathieuValue(first,k)));
            var second=new PrompterStory(Load((Dictionary<string,object>)rows["067_mathieu_02"]),false);second.OnActionTriggered+=(a)=>{};second.StartDialog(flags,"mathieu-02-start");string two=string.Join("\n",Drain(second).ToArray());
            Check(HasText(two,"почти не разговаривали")==!talked && HasText(two,"Его отец умирает в больнице")==talked && MathieuValue(first,"_Mathieu.Talked.Father")== (talked?1:0) && MathieuValue(first,"_Mathieu.Talked.Talked.Father")==0,"Mathieu actual01to02 approved original FRsingleTalked gate with obsolete typo0, talked="+talked);
        }
        {
            var profiles=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> g in (IEnumerable)scenes[3]["profiles"])profiles.Add(g);
            int pairs=0;var used=new HashSet<string>();foreach(Dictionary<string,object> r in (IEnumerable)scenes[2]["routes"]) {
                var v=(Dictionary<string,object>)r["expected_variables"];var family=Strings(reference["family_flags"]);string signature="";foreach(string key in family)signature+=Convert.ToInt32(v[key])+",";if(!used.Add(signature))continue;
                List<string> text,actions;var third=ReferenceRun((Dictionary<string,object>)rows["067_mathieu_03"],r,"mathieu-03-start",null,out text,out actions);var flags=new List<DialogVariable>();foreach(string key in family)flags.Add(new DialogVariable(key,MathieuValue(third,key)));
                var fourth=new PrompterStory(Load((Dictionary<string,object>)rows["067_mathieu_04"]),false);fourth.OnActionTriggered+=(a)=>{};fourth.StartDialog(flags,"mathieu-04-start");string four=string.Join("\n",Drain(fourth).ToArray());
                bool empty=MathieuValue(third,"_Mathieu.Empty")==1,baby=MathieuValue(third,"_Mathieu.Baby.Name")==1;
                bool ok=empty?HasText(four,"люлька показалась пустой"):!HasText(four,"люлька показалась пустой");
                if(empty){fourth.ChooseAnswer(1);Drain(fourth);}var menu=fourth.GetChoices();string first=(string)menu[0].GetType().GetField("_text",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(menu[0]);ok &= first.Contains(baby?"Селеста":"малышка");
                Check(ok,"Mathieu actual completed03to04 family flags preserve Emptypriority and conditionalname "+(++pairs)+" "+signature);
            }
            Check(pairs==profiles.Count,"Mathieu all sixactual03finalfamilyflagprofiles carried throughnative04, not guessed contradictory flags");
        }
    }

    class TpsOption {
        public string Menu;
        public List<string> Text,Passages;
        public Dictionary<string,int> Vars;
    }
    static HashSet<string> tpsRandomBoundaries;
    static int tpsPacingTick;
    static List<string> TpsDrain(PrompterStory story,bool paced) {
        var result=new List<string>();int n=0;
        while(story.can_continue) {
            if(++n>500)throw new Exception("TPS native segment loop");
            string line=story.GetNextLine();if(line==null)continue;result.Add(line);
            // RuntimeUtil.ResolveLink constructs System.Random() for every ??
            // link. CPU-speed playback correlates time-seeded adjacent rolls.
            // Pace only those transitions; never change the seed or game link.
            if(paced && tpsRandomBoundaries.Contains(line))System.Threading.Thread.Sleep(20+(tpsPacingTick++*37)%41);
        }
        return result;
    }
    static Dictionary<string,int> TpsVars(object data) {
        var result=new Dictionary<string,int>();foreach(var p in (Dictionary<string,object>)data)result[p.Key]=Convert.ToInt32(p.Value);return result;
    }
    static int TpsValue(Dictionary<string,int> vars,string name) {int n;return vars.TryGetValue(name,out n)?n:0;}
    static string TpsLink(string link,Dictionary<string,int> vars) {
        foreach(string part in link.Split(new string[]{";;"},StringSplitOptions.None)) {
            int split=part.IndexOf('?');if(split<0)return part;
            var m=Regex.Match(part.Substring(0,split),@"^(.+?)(<=|>=|=|<|>)(-?\d+)$");if(!m.Success)throw new Exception("TPS reference condition parse");
            int a=TpsValue(vars,m.Groups[1].Value),b=Int32.Parse(m.Groups[3].Value);string op=m.Groups[2].Value;
            if(op=="="?a==b:op=="<"?a<b:op==">"?a>b:op=="<="?a<=b:a>=b)return part.Substring(split+1);
        }throw new Exception("TPS reference conditional missing fallback");
    }
    static void TpsExpand(Dictionary<string,object> blocks,string title,Dictionary<string,int> previous,List<string> text,List<string> path,List<TpsOption> results) {
        if(path.Count>100)throw new Exception("TPS reference segment loop");
        if(title=="END") {results.Add(new TpsOption{Menu=title,Vars=previous,Text=text,Passages=path});return;}
        var p=(Dictionary<string,object>)blocks[title];var vars=new Dictionary<string,int>(previous);
        foreach(string modifier in Strings(p["modifiers"])) {
            var m=Regex.Match(modifier,@"^(.+?)(\+=|-=|=)(-?\d+)$");if(!m.Success)throw new Exception("TPS reference modifier parse");
            string key=m.Groups[1].Value,op=m.Groups[2].Value;int n=Int32.Parse(m.Groups[3].Value);
            vars[key]=op=="="?n:TpsValue(vars,key)+(op=="+="?n:-n);
        }
        var nextText=new List<string>(text);nextText.AddRange(Strings(p["texts"]));var nextPath=new List<string>(path);nextPath.Add(title);
        var choices=new List<object>();foreach(object c in (IEnumerable)p["choices"])choices.Add(c);
        if(choices.Count>0) {results.Add(new TpsOption{Menu=title,Vars=vars,Text=nextText,Passages=nextPath});return;}
        string link=(string)p["link"];
        if(link.StartsWith("??"))foreach(string target in link.Substring(2).Split('|'))TpsExpand(blocks,target,vars,nextText,nextPath,results);
        else TpsExpand(blocks,TpsLink(link,vars),vars,nextText,nextPath,results);
    }
    static TpsOption TpsMatch(PrompterStory story,Dictionary<string,object> blocks,string start,Dictionary<string,int> vars,List<string> actual) {
        var expected=new List<TpsOption>();TpsExpand(blocks,start,vars,new List<string>(),new List<string>(),expected);
        foreach(var e in expected) {
            bool same=actual.Count==e.Text.Count;if(same)for(int i=0;i<actual.Count;i++)same &= actual[i]==e.Text[i];
            if(!same)continue;
            foreach(var p in e.Vars) {var v=story.FindVariable(p.Key);if(v==null || v.value!=p.Value){same=false;break;}}
            if(!same)continue;
            var choices=story.GetChoices();var menu=new List<Dictionary<string,object>>();
            if(e.Menu!="END")foreach(Dictionary<string,object> c in (IEnumerable)((Dictionary<string,object>)blocks[e.Menu])["choices"])menu.Add(c);
            if(menu.Count!=choices.Count)continue;
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            for(int i=0;i<menu.Count;i++)same &= (string)choices[i].GetType().GetField("_text",flags).GetValue(choices[i])==(string)menu[i]["text"] && (string)choices[i].GetType().GetField("_link",flags).GetValue(choices[i])==(string)menu[i]["link"];
            if(same)return e;
        }
        throw new Exception("TPS native output/menu/state outside independent original graph from "+start);
    }
    static void TpsActions(Dictionary<string,object> blocks,List<string> passages,List<string> actual) {
        var expected=new List<string>();foreach(string p in passages)foreach(string raw in Strings(((Dictionary<string,object>)blocks[p])["commands"])) {
            string c=raw.Trim();if(!c.StartsWith("$$")||!c.EndsWith("$$"))throw new Exception("TPS original command wrapper");expected.Add(c.Substring(2,c.Length-4).Trim());
        }
        AngeliqueActions(new Dictionary<string,object>{{"expected_actions",expected}},actual);
    }
    static void TpsHealthThreshold(Dictionary<string,object> row,Dictionary<string,object> blocks,Dictionary<string,int> initial) {
        const string start="tps-01-game-xenofex-fight-rules-life-driver-health14";
        foreach(int love in new int[]{0,1}) {
            Dictionary<string,int> vars;List<string> actions;var story=TpsStart(row,start,initial,new Dictionary<string,int>{{"driver-health",14},{"shaine-love",love}},out vars,out actions);
            var expected=new List<string>(Strings(((Dictionary<string,object>)blocks[start])["texts"]));
            string branch=start+(love==0?"-outro":"-help");expected.AddRange(Strings(((Dictionary<string,object>)blocks[branch])["texts"]));expected.AddRange(Strings(((Dictionary<string,object>)blocks["tps-01-game-xenofex-fight-rules-outro"])["texts"]));
            var actual=new List<string>();int steps=0;while(actual.Count<expected.Count&&story.can_continue) {if(++steps>60)throw new Exception("TPS threshold prefix loop");string line=story.GetNextLine();if(line!=null)actual.Add(line);}
            bool ok=actual.Count==expected.Count;for(int i=0;i<actual.Count&&i<expected.Count;i++)ok &= actual[i]==expected[i];int atBoundary=story.FindVariable("driver-health").value;
            ok &= atBoundary==(love==0?14:19);
            // Drain continues into the next randomly selected combat event.
            // Compare that whole tail too, but do not mistake its final health
            // for the earlier five-point support effect.
            actual.AddRange(Drain(story));var matched=TpsMatch(story,blocks,start,vars,actual);TpsActions(blocks,matched.Passages,actions);
            Check(ok,"TPS controlled14health support threshold love="+love+" boundaryhealth="+atBoundary+" tailhealth="+story.FindVariable("driver-health").value+"; next RNG tail also matched");
        }
    }
    static PrompterStory TpsStart(Dictionary<string,object> row,string start,Dictionary<string,int> initial,Dictionary<string,int> overrides,out Dictionary<string,int> vars,out List<string> actions) {
        vars=new Dictionary<string,int>(initial);var dialog=Load(row);var globals=new List<DialogVariable>();
        if(overrides!=null)foreach(var p in overrides) {
            vars[p.Key]=p.Value;bool found=false;
            foreach(var v in (List<DialogVariable>)dialog.GetType().GetField("_variables",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(dialog))if(v.name==p.Key){Set(v,"_value",p.Value);found=true;}
            if(!found)globals.Add(new DialogVariable(p.Key,p.Value));
        }
        var emitted=new List<string>();actions=emitted;var story=new PrompterStory(dialog,false);story.OnActionTriggered+=(a)=>emitted.Add(a.action);story.StartDialog(globals,start);return story;
    }
    static void TpsChecks(Dictionary<string,object> row) {
        var reference=ReferenceFile("localization/tps_validation_reference.json");var blocks=(Dictionary<string,object>)reference["blocks"];var initial=TpsVars(reference["initial"]);
        tpsRandomBoundaries=new HashSet<string>();foreach(var pair in blocks) {
            var p=(Dictionary<string,object>)pair.Value;var lines=Strings(p["texts"]);
            if(((string)p["link"]).StartsWith("??") && lines.Count>0)tpsRandomBoundaries.Add(lines[lines.Count-1]);
        }
        int direct=0,answerCount=0;
        foreach(var pair in blocks) {
            Dictionary<string,int> vars;List<string> actions;var story=TpsStart(row,pair.Key,initial,null,out vars,out actions);var text=Drain(story);var matched=TpsMatch(story,blocks,pair.Key,vars,text);
            TpsActions(blocks,matched.Passages,actions);
            Check(matched.Passages.Contains(pair.Key),"TPS controlled paragraph exact native text/menu/state/orderedcommands "+(++direct)+" "+pair.Key);
            var p=(Dictionary<string,object>)pair.Value;var menu=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)p["choices"])menu.Add(c);
            for(int i=0;i<menu.Count;i++) {
                story=TpsStart(row,pair.Key,initial,null,out vars,out actions);matched=TpsMatch(story,blocks,pair.Key,vars,Drain(story));
                if(matched.Menu!=pair.Key)throw new Exception("TPS controlled answer changed starting menu");
                story.ChooseAnswer(i);var tail=TpsMatch(story,blocks,(string)menu[i]["link"],matched.Vars,Drain(story));
                var fullPath=new List<string>(matched.Passages);fullPath.AddRange(tail.Passages);TpsActions(blocks,fullPath,actions);
                Check(tail!=null,"TPS controlled answer preserves source edge and emitted text/state "+(++answerCount)+" "+pair.Key+"/"+i);
            }
        }
        Check(direct==208 && answerCount==104,"TPS all208passages/104answers controlled including10 default-unreachable blocks; not normal-playthrough coverage");
        var seen=new HashSet<string>();var seenEdges=new HashSet<string>();var use=new Dictionary<string,int>();int journeys=0;
        var focused=ReferenceFile("localization/tps_rare_ordinary_policy.json");
        if((string)focused["source_sha256"]!=(string)reference["source_sha256"])throw new Exception("TPS focused policy source changed");
        var focusedAnswers=(Dictionary<string,object>)focused["answers"];int focusedJourneys=0,focusedTargetHits=0;
        var expectedPassages=new HashSet<string>(Strings(reference["default_passages"]));var expectedEdges=new HashSet<string>(Strings(reference["default_edges"]));
        for(int run=0;run<5000;run++) {
            // Global least-used menu balancing can correlate earlier choices
            // and starve a rare health profile. Every fourth ordinary journey
            // uses source-derived player answers. Never override variables,
            // starting passage, random seeds, links or random outcomes.
            bool useFocused=run%4==3;if(useFocused)focusedJourneys++;
            Dictionary<string,int> vars;List<string> actions;var story=TpsStart(row,"tps-01-start",initial,null,out vars,out actions);string start="tps-01-start";int menus=0;var visited=new List<string>();
            while(true) {
                var matched=TpsMatch(story,blocks,start,vars,TpsDrain(story,true));vars=matched.Vars;foreach(string p in matched.Passages){seen.Add(p);visited.Add(p);}
                if(matched.Menu=="END")break;if(++menus>40)throw new Exception("TPS normal journey menu loop");
                var choices=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)((Dictionary<string,object>)blocks[matched.Menu])["choices"])choices.Add(c);
                int index=0,min=Int32.MaxValue;for(int i=0;i<choices.Count;i++){string key=matched.Menu+"/"+i;int count=use.ContainsKey(key)?use[key]:0;if(count<min){index=i;min=count;}}
                if(useFocused && focusedAnswers.ContainsKey(matched.Menu))index=Convert.ToInt32(focusedAnswers[matched.Menu]);
                // A focused policy must not make a balanced answer look used:
                // its prefix can be far more frequent than the balanced one.
                string edge=matched.Menu+"/"+index;if(!useFocused)use[edge]=(use.ContainsKey(edge)?use[edge]:0)+1;seenEdges.Add(edge);start=(string)choices[index]["link"];story.ChooseAnswer(index);
            }
            var sourceCommands=new List<string>();foreach(string p in visited)sourceCommands.AddRange(Strings(((Dictionary<string,object>)blocks[p])["commands"]));
            TpsActions(blocks,visited,actions);
            bool ok=expectedPassages.IsSupersetOf(visited) && !visited.Contains("tps-01-game-xenofex-pierrealive") && !visited.Contains("tps-01-end-dem-used");
            foreach(string token in new string[]{"travel: start","travel: stop","tip:","clue:","destination:"})ok &= actions.FindAll(a=>a.Contains(token)).Count==sourceCommands.FindAll(c=>c.Contains(token)).Count;
            bool ring=visited.Contains("tps-01-game-xenofex-fight-dem-scene02b"),crisis=visited.Contains("tps-01-crisis"),noGame=visited.Contains("tps-01-nogame-end");
            ok &= story.FindVariable("xenofex-dead").value==(ring?1:0) && story.FindVariable("driver-dead").value==(ring?1:0);
            ok &= story.FindVariable("xenofex-revealed").value==(ring||crisis?1:0) && visited.Contains("tps-01-end-brother")== (ring||crisis);
            ok &= noGame?story.FindVariable("shaine-love").value==0:story.FindVariable("shaine-love").value>=1;
            Check(ok,"TPS complete actual random journey exact original segments/menus/variables, game versus real brother and travel/tips "+(++journeys));
            if(useFocused && visited.Contains((string)focused["target"]))focusedTargetHits++;
            if(run>=159 && seen.SetEquals(expectedPassages) && seenEdges.SetEquals(expectedEdges))break;
        }
        if(!seen.SetEquals(expectedPassages) || !seenEdges.SetEquals(expectedEdges)) {
            foreach(string p in expectedPassages)if(!seen.Contains(p))Console.WriteLine("TPS MISSING PARAGRAPH "+p);
            foreach(string e in expectedEdges)if(!seenEdges.Contains(e))Console.WriteLine("TPS MISSING EDGE "+e);
        }
        Check(seen.SetEquals(expectedPassages) && seenEdges.SetEquals(expectedEdges),"TPS actual default full journeys cover198reachablepassages/101answers; independent graph131714states, not every random combination; journeys="+journeys);
        Check(focusedJourneys>0,"TPS focused ordinary player-answer journeys="+focusedJourneys+"; actual rare target hits="+focusedTargetHits+"; unchanged default start/state/RNG, not controlled entry");
        TpsHealthThreshold(row,blocks,initial);
        foreach(string start in new string[]{"tps-01-game-xenofex-fight-turn01x-damage-low","tps-01-game-xenofex-fight-turn01x-damage-mid","tps-01-game-xenofex-fight-turn01x-damage-high"}) {
            Dictionary<string,int> vars;List<string> actions;var story=TpsStart(row,start,initial,null,out vars,out actions);TpsMatch(story,blocks,start,vars,Drain(story));
            Check(story.FindVariable("driver-health").value==29,"TPS original spokenXenofexdamage has no actualhealthmodifier "+start);
        }
        {
            Dictionary<string,int> vars;List<string> actions;var story=TpsStart(row,"tps-01-game-xenofex-fight-dem-scene03",initial,new Dictionary<string,int>{{"pierre-heatlh",1}},out vars,out actions);
            var matched=TpsMatch(story,blocks,"tps-01-game-xenofex-fight-dem-scene03",vars,Drain(story));
            Check(matched.Passages.Contains("tps-01-game-xenofex-pierrealive"),"TPS controlled external typo variable opens survival; default declaredpierre-health alone does not");
        }
        {
            Dictionary<string,int> vars;List<string> actions;var story=TpsStart(row,"tps-01-game-xenofex-fight-driver-killed-pierre",initial,new Dictionary<string,int>{{"driver-health",0}},out vars,out actions);
            TpsMatch(story,blocks,"tps-01-game-xenofex-fight-driver-killed-pierre",vars,Drain(story));
            Check(story.FindVariable("pierre-dead").value==1 && story.FindVariable("driver-dead").value==1 && story.FindVariable("pierre-health").value==100,"TPS original wrongkilledcharacter condition/flag preserved; driver subsequently dies inring");
        }
        var metadata=Strings(row["_user_datas"]);
        Check(metadata.Count==15&&metadata[0]=="passengercall: «Мы едем… на площадь Этуаль, месье»."&&metadata.FindAll(x=>x.StartsWith("anim-link:")).Count==14,"TPS original useful French Etoile request and14animations; sourcehascall0 does not suppress translated invitation");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2389.json");
        Check((string)card["fullname"]=="Pierre-Henri de Préhaut & Shaïne Patel-Marquand"&&Convert.ToInt32(card["age"])==17&&Convert.ToInt32(card["gender"])==4,"TPS original mixed groupcard17; individual gender from scene, not card/name; no unverified citizenship or romance");
    }

    static void NinaChecks(Dictionary<string,object> row) {
        var reference=ReferenceFile("localization/nina_validation_routes.json");var covered=new HashSet<string>();var edges=new HashSet<string>();int number=0;
        foreach(object group in (IEnumerable)reference["groups"]) {
            var alternatives=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> r in (IEnumerable)group)alternatives.Add(r);
            List<string> text,actions;var story=ReferenceRun(row,alternatives[0],"Nina-01-start",null,out text,out actions,false);Dictionary<string,object> matched=null;
            foreach(var r in alternatives) {
                var expected=Strings(r["expected_texts"]);bool same=text.Count==expected.Count;
                if(same)for(int i=0;i<text.Count;i++)same &= text[i]==expected[i];if(same){matched=r;break;}
            }
            if(matched==null)throw new Exception("Nina actual random output not an allowed exact complete source path");
            ReferenceCoverage(matched,covered,edges);var p=new HashSet<string>(Strings(matched["passages"]));string all=string.Join("\n",text.ToArray());
            bool quiet=p.Contains("Nina-01-start-silence"),earlyStop=p.Contains("Nina-01-end-runaway-stop-intro");
            int rage=(p.Contains("Nina-01-story2-nolove") || p.Contains("Nina-01-story2-dontknow")?1:0)+(p.Contains("Nina-01-story4")?1:0)+(p.Contains("Nina-01-story4-insult")?1:0);
            bool ok=story.FindVariable("rage").value==rage && rage<=3 && story.FindVariable("heat").value==(p.Contains("Nina-01-start-heat")?1:0);
            ok &= HasText(all,"Потеплее не надо")==p.Contains("Nina-01-story1-heat");
            ok &= HasText(all,"Бриса")==quiet && HasText(all,"я родилась в Сарселе")==quiet && HasText(all,"мне кажется, он мне изменял")==quiet;
            ok &= HasText(all,"кольцо в бокал шампанского")==!quiet && HasText(all,"Снимает туфли")==earlyStop;
            ok &= HasText(all,"Никогда не повторяйте")==!earlyStop && !HasText(all,"не открывается из-за блокировки");
            ok &= HasText(all,"сточной канаве")==earlyStop && HasText(all,"собственное предложение руки")==p.Contains("Nina-01-end-memory");
            var tips=actions.FindAll(a=>a.Contains("tip"));ok &= tips.Count==(earlyStop?1:2) && tips[0].Contains("medium") && (earlyStop || tips[1].Contains("high"));
            ok &= actions.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && actions.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
            ok &= actions.FindAll(a=>a.Contains("destination")).Count==(earlyStop?0:1) && actions.FindAll(a=>a.Contains("065_02")).Count==0;
            ok &= actions.FindAll(a=>a.Contains("065_01")).Count==(quiet?1:0) && actions.FindAll(a=>a.Contains("065_03")).Count==(earlyStop?0:1);
            ok &= actions.FindAll(a=>a.Contains("065_04")).Count==(p.Contains("Nina-01-end-restaurant-late")?1:0) && actions.FindAll(a=>a.Contains("065_05")).Count==(p.Contains("Nina-01-end-restaurant-ontime")?1:0);
            ok &= actions.FindAll(a=>a.Contains("clue:") || a.Contains("fare")).Count==0 && !story.can_continue && story.GetChoices().Count==0;
            Check(ok,"Nina exact complete menu path preserves rejected proposal, Brice testimony, heat misunderstanding, rose fate and source rage limit "+(++number));
        }
        Check(number==460 && covered.Count==34 && edges.Count==25 && covered.Contains("Nina-01-end-restaurant-late") && covered.Contains("Nina-01-end-restaurant-ontime"),"Nina460full default menu paths cover34reachable passages/25answers; both real native random rose endings observed, two rage-only blocks separate");
        for(int probe=0;probe<3;probe++) {
            int rage=probe==0?3:4;var d=Load(row);
            foreach(DialogVariable v in (IEnumerable)d.GetType().GetField("_variables",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(d))if(v.name=="rage")Set(v,"_value",rage);
            var story=Start(d,"Nina-01-end-hub");var actions=new List<string>();story.OnActionTriggered+=(a)=>actions.Add(a.action);var text=Drain(story);string first=string.Join("\n",text.ToArray());
            bool ok=HasText(first,"не открывается из-за блокировки")== (rage>3) && HasText(first,"попросить вас об одолжении")== (rage==3);
            story.ChooseAnswer(probe==2?1:0);text.AddRange(Drain(story));int count=0;
            while(story.GetChoices().Count>0){if(++count>5)throw new Exception("Nina controlled tail loop");story.ChooseAnswer(0);text.AddRange(Drain(story));}
            ok &= HasText(string.Join("\n",text.ToArray()),"Хочу выйти. Сейчас")== (probe==1) && story.FindVariable("rage").value==rage && !story.can_continue;
            Check(ok,"Nina controlled original rage>3 threshold/tail preserves3restaurant and4runaway; not default reachability "+rage+"/"+probe);
        }
    }

    static PrompterStory Flags(Dialog d,string passage,string killer) {
        var flags=new List<DialogVariable>();
        foreach(string name in new string[]{"Judge","Sandman","Angel"})flags.Add(new DialogVariable("_Killer.Is."+name,name==killer?1:0));
        flags.Add(new DialogVariable("_Found.Clue.Today",0));flags.Add(new DialogVariable("_Found.Document.Today",0));
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};story.StartDialog(flags,passage);return story;
    }
    static PrompterStory JacquieFlags(Dialog d,string passage,int clues,int clueDialogue,int seen) {
        var flags=new List<DialogVariable>{new DialogVariable("_Jacquie.Gave.Clue",clues),
            new DialogVariable("_Jacquie.Clue.Dialogue",clueDialogue),new DialogVariable("_Jacquie.Story03.Seen",seen),
            new DialogVariable("_Found.Clue.Today",0),new DialogVariable("_Driver.Met.Jacquie",0)};
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};story.StartDialog(flags,passage);return story;
    }
    static PrompterStory ApollonieFlags(Dialog d,int rumors) {
        var flags=new List<DialogVariable>{new DialogVariable("_Rumor.Furiosa",(rumors&1)!=0?1:0),
            new DialogVariable("_Rumor.Napo",(rumors&2)!=0?1:0),new DialogVariable("_Rumor.Soliles",(rumors&4)!=0?1:0),
            new DialogVariable("_Driver.Met.Apollonie",0),new DialogVariable("_Apollonie.Met.SameDay",0),new DialogVariable("_Apollonie.Met.Belvedere",0)};
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};story.StartDialog(flags,"apollonie-start");return story;
    }
    static int[] childericObservedProfiles;
    static void ChildericChecks(Dictionary<string,object> rows) {
        string ct;childericObservedProfiles=new int[2];
            int chRoutes=0;
            for(int chHugo=0;chHugo<2;chHugo++)chRoutes+=ChildericRoutes((Dictionary<string,object>)rows["052_childeric_01"],chHugo,new List<int>());
            Check(chRoutes>0,"Childeric all reachable answer paths with and without prior Hugo meeting: "+chRoutes);
            for(int chHugo=0;chHugo<2;chHugo++) {
                var isolated=ChildericStart(Load((Dictionary<string,object>)rows["052_childeric_01"]),"childeric-01-drive1-yourhorse-costume",chHugo);
                ct=string.Join("\n",Drain(isolated).ToArray());
                Check(HasText(ct,"не костюм, а доспех") && HasText(ct,"доспехе Стиллбоса") && isolated.FindVariable("play").value==(chHugo==1?0:-1),"Childeric isolated original costume block preserved; controlled direct start only "+chHugo);
            }

        var reference=ReferenceFile("localization/childeric_hugo_fr_coverage.json");Dictionary<string,object> hugoReference=null,childReference=null;
        foreach(Dictionary<string,object> sr in (IEnumerable)reference["scenes"])if((string)sr["scene"]=="005_hugo_01")hugoReference=sr;else childReference=sr;
        foreach(Dictionary<string,object> profile in (IEnumerable)childReference["profiles"]) {
            int mode=Convert.ToInt32(profile["mode"]);Check(childericObservedProfiles[mode]==Convert.ToInt32(profile["histories"])&&childericObservedProfiles[mode]==144,"Childeric full old recursion equals independent144immutable histories with MetHugo="+mode);
        }
        Check(chRoutes==288&&chRoutes==Convert.ToInt32(childReference["total_histories"])&&Convert.ToInt32(childReference["ordinary_passages"])==32&&Convert.ToInt32(childReference["answers"])==23,"Childeric all288old complete histories retained;32ordinary blocks23answers, one separate source-unlinked costume fragment");
        var row=(Dictionary<string,object>)rows["052_childeric_01"];var data=Strings(row["_user_datas"]);
        Check(data.Count==5&&data[0]=="passengercall: «О, дорогой друг! Мне очень нужна ваша помощь!»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==4,"Childeric French formal help request, original index0/prefix/4animation definitions; no new icons");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2376.json");Check((string)card["fullname"]=="Childéric de Prahan"&&Convert.ToInt32(card["age"])==17&&Convert.ToInt32(card["gender"])==3,"Childeric male17sourcecard and Hugo male by speech, not race noun ending or assumed relationship");
        var seen=new HashSet<string>();var edges=new HashSet<string>();int rides=0,good=0,ordinary=0;
        foreach(Dictionary<string,object> r in (IEnumerable)hugoReference["routes"]) {
            List<string> text,actions;var hu=ReferenceRun((Dictionary<string,object>)rows["005_hugo_01"],r,(string)hugoReference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,edges);var path=Strings(r["passages"]);bool happy=path.Contains("hugo-end-good");if(happy)good++;else ordinary++;
            string all=string.Join("\n",text.ToArray());bool ok=!hu.can_continue&&hu.GetChoices().Count==0&&hu.FindVariable("_Driver.Met.Hugo").value==1&&all.Contains("косплей гравии")&&all.Contains("костюм довольно смелый")&&all.Contains("пресс напоказ");
            ok &= all.Contains("Удачи с твоим гравием")==happy&&all.Contains("забирает сдачу")==!happy;
            if(path.Contains("hugo-late"))ok &= all.Contains("Всё под контролем");
            if(path.Contains("hugo-costume"))ok &= all.Contains("он… особенный")&&all.Contains("Особенный?");
            Check(ok,"Hugo complete source history "+(++rides)+" exact French-aligned text/menu/state/actions, separate race/mishearing and good-versus-ordinary ending");
            var ch=ChildericStart(Load(row),"childeric-01-start",hu.FindVariable("_Driver.Met.Hugo").value);var emitted=new List<string>();ch.OnActionTriggered+=(evt)=>emitted.Add(evt.action);var whole=Drain(ch);
            foreach(int answer in new int[]{2,0,1,1}){ch.ChooseAnswer(answer);whole.AddRange(Drain(ch));}
            string joined=string.Join("\n",whole.ToArray());
            Check(!ch.can_continue&&ch.GetChoices().Count==0&&ch.FindVariable("play").value==3&&joined.Contains("Гравия? Слово кажется знакомым")&&joined.Contains("Чтобы он меня заметил")&&joined.Contains("кроличьими ушами")&&emitted.Exists(x=>x.Contains("052_01"))&&emitted.Exists(x=>x.Contains("052_02")),"Completed005history"+rides+"actual MetHugo flag opens complete052recognition/affection and strictplay3reveal; not all005x052Cartesian histories");
        }
        Check(rides==24&&good==12&&ordinary==12&&seen.Count==13&&edges.Count==9,"Hugo all24finite complete source histories/13blocks9answers/both endings and24actual state handoffs retained");
        data=Strings(((Dictionary<string,object>)rows["005_hugo_01"])["_user_datas"]);Check(data.Count==7&&data[0]=="passengercall: «Гамбетта!!!»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==6,"Hugo source Gambetta invitation kept separate from Gallieni body destination,6original animations/no new icons");
    }
    static int[] amelieObservedModes;
    static void AmelieChecks(Dictionary<string,object> rows) {
        amelieObservedModes=new int[4];
            int amRoutes=0;
            for(int amMode=0;amMode<4;amMode++)amRoutes+=AmelieRoutes((Dictionary<string,object>)rows["047_amelie_01"],amMode,new List<int>());
            Check(amRoutes>0,"Amelie all reachable answer paths in three cases and freeroam: "+amRoutes);

        var reference=ReferenceFile("localization/amelie_fr_coverage.json");int expectedTotal=0;
        foreach(Dictionary<string,object> profile in (IEnumerable)reference["profiles"]) {
            int mode=Convert.ToInt32(profile["mode"]),expected=Convert.ToInt32(profile["histories"]);expectedTotal+=expected;
            Check(amelieObservedModes[mode]==expected&&expected==858,"Amelie original recursive traversal equals independent immutable858histories in mode"+mode);
        }
        Check(amRoutes==expectedTotal&&amRoutes==3432&&Convert.ToInt32(reference["ordinary_passages"])==57&&Convert.ToInt32(reference["answers"])==35,"Amelie all3432finite original complete answer histories across3cases/freeroam retained, independent57blocks35answers; not every random clue choice");
        var row=(Dictionary<string,object>)rows["047_amelie_01"];var data=Strings(row["_user_datas"]);
        Check(data.Count==5&&data[0]=="passengercall: «В четырнадцатый округ… Спасибо»."&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==4,"Amelie original14th district call at index0 and4animation definitions retained; four original ENsilence markers not new drawings");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2374.json");Check((string)card["fullname"]=="Amélie Franconi"&&Convert.ToInt32(card["age"])==29&&Convert.ToInt32(card["gender"])==0,"Amelie source female29card, son gender by text; no inferred child name/age/father or exact career starting age");
    }
    static void ShohrehChecks(Dictionary<string,object> rows) {
        string ct;int profiles=0,handoffs=0;
            int shMetFromCompleted=0;
            for(int shGreeting=0;shGreeting<3;shGreeting++)for(int shGenre=0;shGenre<4;shGenre++)for(int shSound=0;shSound<4;shSound++)for(int shWork=0;shWork<5;shWork++) {
                var sh=ShohrehStart(Load((Dictionary<string,object>)rows["046_shohreh_01"]));var emitted=new List<string>();
                sh.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(sh).ToArray());
                bool coherent=sh.FindVariable("_Driver.Met.Shohreh").value==1 && HasText(ct,"районным полицейским участком") && HasText(ct,"в Монпарнасе");
                sh.ChooseAnswer(shGreeting);ct=string.Join("\n",Drain(sh).ToArray());
                coherent &= HasText(ct,"Вы с трудом слышите её голос")== (shGreeting==0);
                coherent &= HasText(ct,"очень любезна")== (shGreeting==1);
                coherent &= HasText(ct,"украли кошелёк") && HasText(ct,"Деньги на поездку у меня есть") && HasText(ct,"«Хорошо»");
                coherent &= sh.GetChoices().Count==4;
                sh.ChooseAnswer(shGenre);ct=string.Join("\n",Drain(sh).ToArray());
                coherent &= HasText(ct,"Она качает головой") && HasText(ct,"89,4 только помехи") && HasText(ct,"Слышите?");
                coherent &= HasText(sh.GetChoices()[2].GetText(),"припев");
                sh.ChooseAnswer(shSound);ct=string.Join("\n",Drain(sh).ToArray());
                coherent &= emitted.Exists(a=>a.Contains("046_02"))==(shSound!=3) && emitted.Exists(a=>a.Contains("046_01"))==(shSound==3);
                coherent &= HasText(ct,"месье")== (shSound==0);
                coherent &= HasText(ct,"Кит?")== (shSound==1);
                coherent &= HasText(ct,"Припев?")== (shSound==2) && HasText(ct,"Я его не слышу")== (shSound==2);
                coherent &= HasText(ct,"Ритм")== (shSound==3);
                coherent &= HasText(ct,"Доказательство того, что мы существуем");
                sh.ChooseAnswer(shWork<3?0:shWork-2);ct=string.Join("\n",Drain(sh).ToArray());
                if(shWork<3){coherent &= HasText(ct,"Математикой");sh.ChooseAnswer(shWork);ct=string.Join("\n",Drain(sh).ToArray());}
                coherent &= HasText(ct,"Сначала алгебра")== (shWork==0) && HasText(ct,"Линейная алгебра")== (shWork==0);
                coherent &= HasText(ct,"гармонический анализ")== (shWork==0) && HasText(ct,"спектральная теория")== (shWork==0);
                coherent &= HasText(ct,"Нет, совсем нет")== (shWork==1) && HasText(ct,"Занимаюсь исследованиями")== (shWork==1);
                coherent &= HasText(ct,"Я слушала их, когда была маленькой")== (shWork==3) && HasText(ct,"лёгкий акцент")== (shWork==3);
                coherent &= HasText(ct,"Эта частота проникает в ваши мысли")== (shWork==2 || shWork==4);
                coherent &= HasText(ct,"музыка обретает очертания") && HasText(ct,"Я думала, что это призраки") && HasText(ct,"словно детский голос");
                coherent &= HasText(ct,"принимаете деньги") && HasText(ct,"Это вас успокаивает") && !HasText(ct,"Филдс");
                var shTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= shTips.Count==2 && shTips[0].Contains("low") && shTips[1].Contains("high");
                coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
                coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==0 && !sh.can_continue && sh.GetChoices().Count==0;
                shMetFromCompleted=sh.FindVariable("_Driver.Met.Shohreh").value;
                profiles++;
                Check(coherent,"Shohreh full route preserves woman researcher, static/refrain, shared frequency and ambiguous music "+shGreeting+"/"+shGenre+"/"+shSound+"/"+shWork);
            }
            for(int olderMet=0;olderMet<4;olderMet++) {
                var radio=PeterFlags(Load((Dictionary<string,object>)rows["030_peter_01"]),"Peter-01-radio02",olderMet | (shMetFromCompleted<<2),3);
                ct=string.Join("\n",Drain(radio).ToArray());
                bool coherent=shMetFromCompleted==1 && HasText(ct,"Женщину-математика расспрашивают о награде")== (olderMet==0) && !HasText(ct,"Филдс");
                handoffs++;
                Check(coherent,"Shohreh actual meeting flag selects Peter's female medal interview behind Alicia/Julian priority "+olderMet);
            }

        Check(profiles==240&&handoffs==4,"Shohreh all240archived full joint profiles and4completed meeting flag handoffs retained; Peter older flags controlled, not full030ride");
        var row=(Dictionary<string,object>)rows["046_shohreh_01"];var data=Strings(row["_user_datas"]);
        Check(data.Count==1&&data[0]=="passengercall: «В район Монпарнас, пожалуйста…»","Shohreh source Montparnasse request at index0, no animation definitions or newly drawn icons");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2373.json");Check((string)card["fullname"]=="Shohreh Prévert"&&Convert.ToInt32(card["age"])==34&&Convert.ToInt32(card["gender"])==0,"Shohreh original female34card; radio972age33remains distinct, no birthday inferred");
    }
    static void VeroChecks(Dictionary<string,object> rows) {
        string ct;int profiles=0;string[] cases={"Judge","Sandman","Angel"};
            for(int veCase=0;veCase<4;veCase++)for(int veFirst=0;veFirst<4;veFirst++)for(int veSecond=0;veSecond<4;veSecond++)for(int veReply=0;veReply<3;veReply++) {
                if(veFirst==3 && (veSecond!=0 || veReply!=0))continue;
                if(veFirst!=2 && veReply!=0)continue;
                if(veSecond==3 && veReply!=0)continue;
                var ve=Flags(Load((Dictionary<string,object>)rows["045_vero_01"]),"vero-01-start",veCase<3?cases[veCase]:"None");
                var emitted=new List<string>();ve.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(ve).ToArray());
                bool coherent=HasText(ct,"цены на квадратный метр") && HasText(ct,"экран остаётся погашенным") && HasText(ct,"минут через десять");
                var veClues=emitted.FindAll(a=>a.Contains("clue:"));coherent &= veClues.Count==(veCase<3?1:0);
                if(veCase<3)coherent &= veClues[0].Contains(veCase==0?"clue_judge_customer_rumor_":veCase==1?"clue_sand_customer_rumor_":"clue_angel_customer_rumor_");
                coherent &= ve.FindVariable("_Found.Clue.Today").value==(veCase<3?1:0);
                ve.ChooseAnswer(veFirst);ct=string.Join("\n",Drain(ve).ToArray());bool sent=veFirst==2;
                coherent &= ve.FindVariable("message").value==(sent?1:0);
                coherent &= HasText(ct,"сразу отправляет")==sent;
                bool received=sent && veSecond!=3;
                bool closed=veFirst==3 || veSecond==3 || (received && veReply==2);
                if(veFirst!=3) {
                    coherent &= HasText(ct,"понравилась эта девушка") && HasText(ct,"Восемь лет живу в Париже") && HasText(ct,"юридическими фирмами");
                    ve.ChooseAnswer(veSecond);ct=string.Join("\n",Drain(ve).ToArray());
                    coherent &= HasText(ct,"В такси — может быть")== (veSecond==1);
                    coherent &= HasText(ct,"Спасибо, что слушаете")== (veSecond==2);
                    coherent &= HasText(ct,"Подруги постарше")== (veSecond!=3);
                    coherent &= HasText(ct,"Рада, что познакомилась с тобой")==received;
                    coherent &= HasText(ct,"Но, кажется, у нас ничего не выйдет")==received;
                    if(received) {
                        coherent &= ve.GetChoices().Count==3 && HasText(ve.GetChoices()[0].GetText(),"Вы же хотели эмодзи");
                        ve.ChooseAnswer(new int[]{0,2,1}[veReply]);ct=string.Join("\n",Drain(ve).ToArray());
                        coherent &= HasText(ct,"Ваши слова повисают")== (veReply==0) && HasText(ct,"Подловили")== (veReply==0);
                        coherent &= HasText(ct,"Она качает головой")== (veReply==1);
                    }
                }
                coherent &= HasText(ct,"Чаще вправо — выбирает анкеты")==closed;
                coherent &= HasText(ct,"Лишь изредка влево — отклоняет")==closed;
                coherent &= HasText(ct,"Быстро выходит из такси")==closed;
                coherent &= HasText(ct,"того, что нам на самом деле нужно")==!closed;
                coherent &= HasText(ct,"Не торопитесь")==!closed;
                coherent &= HasText(ct,"женщина прощается")== (!closed && sent);
                coherent &= HasText(ct,"Смелее")== (!closed && !sent);
                coherent &= emitted.Exists(a=>a.Contains("045_02"))==received && emitted.Exists(a=>a.Contains("045_01"))==closed;
                coherent &= ve.FindVariable("message").value==(sent?1:0);
                var veTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= veTips.Count==1 && veTips[0].Contains("medium");
                coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==(closed?1:0);
                coherent &= emitted.FindAll(a=>a.Contains("trave:") && a.Contains("stop")).Count==0;
                coherent &= !ve.can_continue && ve.GetChoices().Count==0 && HasText(ct,"провожаете её взглядом");
                profiles++;
                Check(coherent,"Veronique full route preserves women, sent/refused text, emoji, swipe directions and repaired silent-end stop "+veCase+"/"+veFirst+"/"+veSecond+"/"+veReply);
            }

        Check(profiles==76,"Veronique all76archived joint profiles retained across4case settings; every old condition and menu index preserved");
        var row=(Dictionary<string,object>)rows["045_vero_01"];var data=Strings(row["_user_datas"]);
        Check(data.Count==5&&data[0]=="passengercall: «Мне в тринадцатый округ…»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==4,"Veronique original13th district call at index0 and4animation definitions retained; no new icons or images");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2372.json");Check((string)card["fullname"]=="Véronique Peuchat"&&Convert.ToInt32(card["age"])==31&&Convert.ToInt32(card["gender"])==0,"Veronique source female31card; law firms not a proved lawyer profession");
    }
    static void ChrisChecks(Dictionary<string,object> rows) {
        string ct;int profiles=0,refugeProfiles=0;
            bool chRandomFather=false,chRandomFriends=false;
            for(int chDeal=0;chDeal<2;chDeal++)for(int chEarly=0;chEarly<5;chEarly++)for(int chOffer=0;chOffer<3;chOffer++)
            for(int chAsk=0;chAsk<3;chAsk++)for(int chDecision=0;chDecision<3;chDecision++)for(int chHeat=0;chHeat<2;chHeat++) {
                if(chOffer==2 && (chAsk!=0 || chDecision!=0 || chHeat!=0))continue;
                if(chOffer!=2 && chAsk==2 && (chDecision!=0 || chHeat!=0))continue;
                bool getsDecision=chOffer!=2 && chAsk!=2;
                if(getsDecision && chDeal==0 && chDecision==2)continue;
                if(getsDecision && chDecision==0 && chHeat!=0)continue;
                var ch=ChrisStart(Load((Dictionary<string,object>)rows["044_chris_01"]),chDeal);var emitted=new List<string>();
                ch.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(ch).ToArray());
                bool coherent=HasText(ct,"ещё нет двадцати") && HasText(ct,"улица Шуази");
                ch.ChooseAnswer(chEarly<2?0:chEarly==2?1:2);ct=string.Join("\n",Drain(ch).ToArray());
                bool father=ch.FindVariable("father").value==1;bool friends=ch.FindVariable("friends").value==1;
                coherent &= father!=friends;
                if(chEarly<2)coherent &= father;else if(chEarly==2)coherent &= friends;
                if(chEarly>=3){chRandomFather|=father;chRandomFriends|=friends;}
                if(father){ch.ChooseAnswer(chEarly==1 || chEarly==4?1:0);ct=string.Join("\n",Drain(ch).ToArray());}
                coherent &= HasText(ct,"Может, массаж") && HasText(ct,"не верите ни слову");
                coherent &= HasText(ch.GetChoices()[0].GetText(),"Ты что делаешь");
                ch.ChooseAnswer(chOffer);ct=string.Join("\n",Drain(ch).ToArray());
                bool stopped=chOffer==2;
                if(!stopped) {
                    coherent &= HasText(ct,"невольно перешли на «ты»")== (chOffer==0);
                    coherent &= HasText(ct,"вырвалось само собой")== (chOffer==1);
                    coherent &= HasText(ch.GetChoices()[0].GetText(),"тебе лет");
                    ch.ChooseAnswer(chAsk);ct=string.Join("\n",Drain(ch).ToArray());stopped=chAsk==2;
                    if(!stopped) {
                        coherent &= HasText(ct,"Двадцать один")== (chAsk==0) && HasText(ct,"Девятнадцать")== (chAsk==0);
                        coherent &= HasText(ct,"Я… да, иногда")== (chAsk==1);
                        coherent &= HasText(ct,"На массаж и… остальное я не согласен") && HasText(ct,"Из общей квартиры меня выгнали");
                        coherent &= ch.GetChoices().Count==(chDeal==1?3:2) && HasText(ch.GetChoices()[1].GetText(),"Продолжить поездку");
                        ch.ChooseAnswer(chDecision);ct=string.Join("\n",Drain(ch).ToArray());stopped=chDecision==0;
                    }
                }
                bool hotel=!stopped && chDecision==2;
                if(!stopped) {
                    coherent &= HasText(ct,"Был с одним мужчиной")==father && HasText(ct,"Был с женщиной")==friends;
                    coherent &= HasText(ct,"У нас даже секса нет") && HasText(ct,"Везуле");
                    coherent &= HasText(ct,"Она мне платит")==friends;
                    coherent &= ch.FindVariable("carolina").value==(hotel?1:0);
                    coherent &= HasText(ct,"Вы меняете место назначения")==hotel && HasText(ct,"Ничего не ждёт взамен")==hotel;
                    coherent &= HasText(ct,"Нет, не она. Но кто-то похожий")==hotel;
                    ch.ChooseAnswer(chHeat);ct=string.Join("\n",Drain(ch).ToArray());
                    coherent &= ch.FindVariable("heat").value==(chHeat==0?1:0);
                    coherent &= HasText(ct,"Он не спит") && HasText(ct,"чуть расстегнул рубашку")== (chHeat==0);
                    coherent &= HasText(ct,"дрожит от холода")== (chHeat==1);
                    coherent &= HasText(ct,"Придётся вернуться домой. Поговорить с родителями") && HasText(ct,"вы уже такое видели");
                    coherent &= HasText(ct,"Каролины")==hotel && HasText(ct,"приветливо встречает")==hotel;
                    coherent &= HasText(ct,"Социальное жильё")==!hotel;
                } else coherent &= HasText(ct,"ты выйдешь здесь") && HasText(ct,"звенит в ушах");
                coherent &= emitted.Exists(a=>a.Contains("044_01"))==stopped;
                coherent &= emitted.Exists(a=>a.Contains("044_02"))==hotel;
                coherent &= emitted.Exists(a=>a.Contains("044_03"))==(!stopped && !hotel);
                coherent &= emitted.FindAll(a=>a.Contains("destination")).Count==(hotel?1:0);
                coherent &= emitted.FindAll(a=>a.Contains("fare")).Count==1 && emitted.Exists(a=>a.Contains("fare") && a.Contains("0"));
                coherent &= emitted.FindAll(a=>a.Contains("tip")).Count==0 && emitted.FindAll(a=>a.Contains("clue:")).Count==0;
                coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
                coherent &= !ch.can_continue && ch.GetChoices().Count==0;
                profiles++;
                Check(coherent,"Chris complete route preserves adult age, tu/vous, distinct clients, decision meaning and conditional refuge "+chDeal+"/"+chEarly+"/"+chOffer+"/"+chAsk+"/"+chDecision+"/"+chHeat);
            }
            Check(chRandomFather && chRandomFriends,"Chris original random silence selected both father and friends in sampled full routes");
            for(int refuge=0;refuge<2;refuge++) {
                var ca=CarolinaStart(Load((Dictionary<string,object>)rows["028_carolinaj_01"]),refuge==1?"carolina-answer-yes":"carolina-answer-no",3,0);Drain(ca);
                int actualDeal=ca.FindVariable("_Driver.Deal.Carolina").value;
                var ch=ChrisStart(Load((Dictionary<string,object>)rows["044_chris_01"]),actualDeal);Drain(ch);
                ch.ChooseAnswer(0);Drain(ch);ch.ChooseAnswer(0);Drain(ch);ch.ChooseAnswer(0);Drain(ch);ch.ChooseAnswer(0);Drain(ch);
                bool coherent=actualDeal==refuge && ch.GetChoices().Count==(refuge==1?3:2);
                ch.ChooseAnswer(refuge==1?2:1);Drain(ch);ch.ChooseAnswer(0);ct=string.Join("\n",Drain(ch).ToArray());
                coherent &= HasText(ct,"приветливо встречает")== (refuge==1) && HasText(ct,"Социальное жильё")== (refuge==0) && !ch.can_continue;
                refugeProfiles++;
                Check(coherent,"Carolina actual accepted/refused deal selects Chris refuge availability and destination "+refuge);
            }

        Check(profiles==190&&refugeProfiles==2,"Chris all190old joint profiles and2original Carolina accepted/refused ending-fragment flag controls retained, plus original actual RNG both-family coverage; not every random Cartesian history");
        var row=(Dictionary<string,object>)rows["044_chris_01"];var data=Strings(row["_user_datas"]);
        Check(data.Count==4&&data[3]=="passengercall: «Тринадцатый округ… Пожалуйста…»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==3,"Chris useful original request remains at index3 after3source animation definitions; no new icons/animations");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2371.json");Check((string)card["fullname"]=="Christophe Clairouin"&&Convert.ToInt32(card["age"])==19&&Convert.ToInt32(card["gender"])==3,"Chris source male19card,21correctedto19intext; child/soldier comparison not a different age or priest012identity");
    }
    static void JonasChecks(Dictionary<string,object> rows) {
        string ct;int profiles=0;
            for(int joHello=0;joHello<2;joHello++)for(int joJob=0;joJob<3;joJob++)for(int joFaith=0;joFaith<9;joFaith++)
            for(int joReality=0;joReality<2;joReality++)for(int joUnderstand=0;joUnderstand<2;joUnderstand++)for(int joOpinion=0;joOpinion<4;joOpinion++)
            for(int joWhy=0;joWhy<2;joWhy++)for(int joFinal=0;joFinal<4;joFinal++) {
                var jo=JonasStart(Load((Dictionary<string,object>)rows["043_jonas_01"]));var emitted=new List<string>();
                jo.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(jo).ToArray());
                bool coherent=jo.FindVariable("_Driver.Met.Jonas").value==1 && HasText(ct,"ноутбук");
                jo.ChooseAnswer(joHello);ct=string.Join("\n",Drain(jo).ToArray());coherent &= HasText(ct,"дежурный ответ")== (joHello==0);
                jo.ChooseAnswer(joJob);ct=string.Join("\n",Drain(jo).ToArray());
                coherent &= emitted.Exists(a=>a.Contains("043_01"))==(joJob==1);
                coherent &= HasText(ct,"Пишу код")== (joJob==0) && HasText(ct,"Как вы догадались")== (joJob==1);
                jo.ChooseAnswer(joFaith<2?joFaith+1:0);ct=string.Join("\n",Drain(jo).ToArray());
                coherent &= HasText(ct,"Раньше я верил")== (joFaith>=2);
                if(joFaith>=2) {
                    jo.ChooseAnswer(joFaith<5?joFaith-1:0);ct=string.Join("\n",Drain(jo).ToArray());
                    if(joFaith>=5){coherent &= HasText(ct,"В Аллаха");jo.ChooseAnswer(joFaith-5);ct=string.Join("\n",Drain(jo).ToArray());}
                }
                coherent &= emitted.Exists(a=>a.Contains("043_02"))==(joFaith>=2) && jo.FindVariable("allah").value==(joFaith>=5?1:0);
                coherent &= HasText(ct,"перестал верить задолго")== (joFaith==5);
                coherent &= HasText(ct,"не ваше дело")== (joFaith==6);
                coherent &= HasText(ct,"Религия вас не отпускает")== (joFaith==7);
                coherent &= HasText(ct,"спрашиваете об этом у людей")== (joFaith==4 || joFaith==8);
                coherent &= jo.GetChoices().Count==2 && HasText(jo.GetChoices()[0].GetText(),"В смысле?");
                jo.ChooseAnswer(joReality);ct=string.Join("\n",Drain(jo).ToArray());
                coherent &= jo.FindVariable("_Found.Clue.Today").value==1 && emitted.Exists(a=>a.Contains("clue_angel_customer_jonas_01"));
                jo.ChooseAnswer(joUnderstand);ct=string.Join("\n",Drain(jo).ToArray());
                coherent &= HasText(ct,"Он поправляется")== (joUnderstand==0);
                coherent &= HasText(ct,"компьютерщик") && !HasText(ct,"компьютерная программа");
                jo.ChooseAnswer(joOpinion);ct=string.Join("\n",Drain(jo).ToArray());
                coherent &= HasText(ct,"выдернет вилку")== (joOpinion==0);
                coherent &= HasText(ct,"заметно оживившись")== (joOpinion==2);
                coherent &= HasText(ct,"Мы есть") && HasText(ct,"Нас больше нет") && HasText(ct,"пытаюсь выяснить");
                jo.ChooseAnswer(joWhy);ct=string.Join("\n",Drain(jo).ToArray());
                coherent &= HasText(ct,"удовольствие от наших мучений")== (joWhy==0);
                coherent &= HasText(ct,"значение с плавающей точкой") && HasText(ct,"Он негромко смеётся");
                jo.ChooseAnswer(joFinal);ct=string.Join("\n",Drain(jo).ToArray());
                coherent &= HasText(ct,"Гипатия, Коперник, Галилей, Ньютон")== (joFinal==0);
                coherent &= HasText(ct,"атеист заявит")== (joFinal==1);
                coherent &= HasText(ct,"Бога, Аллаха или кого-то ещё")== (joFinal==1 && joFaith<5);
                coherent &= HasText(ct,"что Аллаха не существует")== (joFinal==1 && joFaith>=5);
                coherent &= HasText(ct,"правильный ответ")== (joFinal==2);
                coherent &= HasText(ct,"Увидите, что я прав")== (joFinal==3);
                coherent &= HasText(ct,"ноутбуком под мышкой")== (joFinal!=3);
                var joTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= joTips.Count==(joFinal==3?1:0);if(joFinal==3)coherent &= joTips[0].Contains("none");
                coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
                coherent &= !jo.can_continue && jo.GetChoices().Count==0;
                profiles++;
                Check(coherent,"Jonas complete route preserves former driver faith, speculative float theory, unconditional clue and exact endings "+joHello+"/"+joJob+"/"+joFaith+"/"+joReality+"/"+joUnderstand+"/"+joOpinion+"/"+joWhy+"/"+joFinal);
            }

        var reference=ReferenceFile("localization/jonas_fr_validation_routes.json");var row=(Dictionary<string,object>)rows["043_jonas_01"];
        Check(profiles==6912&&Convert.ToInt32(reference["full_answer_histories"])==6912,"Jonas all6912old joint profiles retained, equal independently counted finite original histories");
        var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        bool menuOk=true;int menus=0,menuAnswers=0;
        foreach(Dictionary<string,object> m in (IEnumerable)reference["menus"]) {
            menus++;var texts=Strings(m["texts"]);var links=Strings(m["links"]);var current=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)m["passage"]]["_choices"])current.Add(c);
            menuOk &= current.Count==texts.Count&&texts.Count==links.Count;
            for(int i=0;i<current.Count;i++){menuAnswers++;menuOk &= (string)current[i]["_text"]==texts[i]&&(string)current[i]["_link"]==links[i];}
        }
        Check(menuOk&&menus==10&&menuAnswers==30,"Jonas all10source menus/30answer edges retain original icons/French-aligned text");
        var seen=new HashSet<string>();var answers=new HashSet<string>();int rides=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,answers);string all=string.Join("\n",text.ToArray());var path=Strings(r["passages"]);bool silent=path.Contains("jonas-end-bad");
            bool ok=!story.can_continue&&story.GetChoices().Count==0&&all.Contains("компьютерщик")&&!all.Contains("компьютерная программа")&&all.Contains("значение с плавающей точкой")&&all.Contains("Мы есть")&&all.Contains("Нас больше нет")&&all.Contains("И в то же время это наше будущее");
            ok &= story.FindVariable("_Found.Clue.Today").value==1&&actions.FindAll(x=>x.Contains("clue:")).Count==1&&actions.Exists(x=>x.Contains("clue_angel_customer_jonas_01"));
            if(path.Contains("jonas-question-allah2-avoidanswer"))ok &= all.Contains("Религия вас не отпускает — от неё так просто не отделаться");
            if(path.Contains("jonas-theory-crazy"))ok &= all.Contains("Гипатия, Коперник, Галилей, Ньютон");
            if(path.Contains("jonas-theory-knowing-allah"))ok &= story.FindVariable("allah").value==1&&all.Contains("что Аллаха не существует");
            if(path.Contains("jonas-simulation-silence"))ok &= all.Contains("Это переворачивает весь наш образ мыслей, весь наш образ жизни");
            var tips=actions.FindAll(x=>x.Contains("tip:"));ok &= tips.Count==(silent?1:0);if(silent)ok &= tips[0].Contains("none")&&all.Contains("Я больше ни слова не скажу")&&all.Contains("Вы переводите дыхание и трогаетесь");
            Check(ok,"Jonas complete representative ride "+(++rides)+" exact French text/menu/state/orderedactions; former driver faith and creator hypothesis stay scoped, disputed source wording not silently repaired");
        }
        Check(rides==30&&seen.Count==35&&answers.Count==30,"Jonas30representatives cover35passages155lines30answers/two carrier alignments; all6912finite histories separately retained");
        var data=Strings(row["_user_datas"]);Check(data.Count==1&&data[0]=="passengercall: «В Исси»."&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==0,"Jonas original useful French Issy direction retained, no source animation definitions/new icons");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2370.json");Check((string)card["fullname"]=="Jonas Piersson"&&Convert.ToInt32(card["age"])==38&&Convert.ToInt32(card["gender"])==3,"Jonas male38sourcecard; programmer/physicist source fields separate, no nationality from former driver belief or actual simulation claim");
    }
    static void DjenaChecks(Dictionary<string,object> rows) {
        string ct;int profiles1=0,profiles2=0;
            for(int djStart=0;djStart<3;djStart++)for(int djEnd=0;djEnd<3;djEnd++) {
                var dj=Start(Load((Dictionary<string,object>)rows["042_djena_01"]),"djena-01-start");var emitted=new List<string>();
                dj.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(dj).ToArray());
                bool coherent=HasText(ct,"больничной парковки") && HasText(ct,"приколот бейдж");
                dj.ChooseAnswer(djStart);ct=string.Join("\n",Drain(dj).ToArray());
                coherent &= HasText(ct,"Она не отвечает")== (djStart==0);
                coherent &= HasText(ct,"пассажирка крепко спит")== (djStart!=2);
                coherent &= HasText(ct,"усиливаете обогрев") && HasText(ct,"Я проснулась") && HasText(ct,"оплачивает поездку");
                dj.ChooseAnswer(djEnd);ct=string.Join("\n",Drain(dj).ToArray());
                coherent &= HasText(ct,"Не поймёшь, то ли язвит")== (djEnd==1);
                coherent &= HasText(ct,"Если вы так говорите")== (djEnd!=1);
                coherent &= HasText(ct,"уже… мертва") && HasText(ct,"песня в стиле рай") && HasText(ct,"Сегодня ночью я убила пациентку") && HasText(ct,"Случайно");
                coherent &= !HasText(ct,"убила пациента") && HasText(ct,"свет в холле не погаснет сам") && !dj.can_continue;
                var djTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= djTips.Count==1 && djTips[0].Contains("low");
                coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==1 && emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==1;
                profiles1++;
                Check(coherent,"Djenabou first complete route retains heating, tired speech and female-patient confession "+djStart+"/"+djEnd);
            }
            for(int djIntro=0;djIntro<3;djIntro++)for(int djJob=0;djJob<4;djJob++)for(int djAnger=0;djAnger<4;djAnger++)for(int djModest=0;djModest<4;djModest++) {
                if(djIntro==2 && (djJob!=0 || djAnger!=0 || djModest!=0))continue;
                if(djIntro!=2 && djAnger==1 && djModest!=0)continue;
                var dj=Start(Load((Dictionary<string,object>)rows["042_djena_02"]),"djena-02-start");var emitted=new List<string>();
                dj.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(dj).ToArray());
                bool coherent=HasText(ct,"случайно убила пациентку") && !HasText(ct,"убила пациента");
                dj.ChooseAnswer(djIntro);ct=string.Join("\n",Drain(dj).ToArray());
                if(djIntro!=2) {
                    coherent &= HasText(ct,"изнываю от жары")== (djIntro==1) && HasText(ct,"принимать меня за медсестру");
                    dj.ChooseAnswer(djJob);ct=string.Join("\n",Drain(dj).ToArray());
                    coherent &= HasText(ct,"Я врач")== (djJob<=1);
                    coherent &= emitted.Exists(a=>a.Contains("042_01"))==(djJob==0);
                    coherent &= HasText(ct,"я чернокожая") && HasText(ct,"я женщина") && HasText(ct,"Медсёстрам и так столько достаётся") && HasText(ct,"УВАЖЕНИЯ, блядь");
                    dj.ChooseAnswer(djAnger);ct=string.Join("\n",Drain(dj).ToArray());
                    coherent &= HasText(ct,"боксёрский ринг")== (djAnger==1);
                    coherent &= HasText(ct,"Блядь. Спасибо")== (djAnger==0 || djAnger==2);
                    if(djAnger!=1){coherent &= dj.GetChoices().Count==4;dj.ChooseAnswer(djModest);ct=string.Join("\n",Drain(dj).ToArray());}
                }
                bool good=djIntro!=2 && djAnger!=1;
                coherent &= emitted.Exists(a=>a.Contains("042_02"))==good;
                coherent &= HasText(ct,"У меня умерла пациентка")==good && HasText(ct,"моей ровесницей")==good && HasText(ct,"умерла от гриппа")==good;
                coherent &= HasText(ct,"она просто не была в приоритете")==good && HasText(ct,"другие срочные случаи")==good;
                coherent &= HasText(ct,"машет вам рукой")==good;
                coherent &= HasText(ct,"даже не глядя на вас")==!good;
                coherent &= !dj.can_continue && dj.GetChoices().Count==0;
                var djTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= djTips.Count==2 && djTips[0].Contains("low") && djTips[1].Contains(good?"medium":"low");
                coherent &= emitted.FindAll(a=>a.Contains("travel")).Count==0 && emitted.FindAll(a=>a.Contains("clue:")).Count==0;
                profiles2++;
                Check(coherent,"Djenabou second complete route distinguishes doctor/racial-gender stereotypes, silence timing and patient's death "+djIntro+"/"+djJob+"/"+djAnger+"/"+djModest);
            }

        var reference=ReferenceFile("localization/djena_fr_validation_routes.json");int expectedTotal=0;foreach(Dictionary<string,object> r in (IEnumerable)reference["scenes"])expectedTotal+=Convert.ToInt32(r["full_answer_histories"]);
        Check(profiles1==9&&profiles2==105&&expectedTotal==114,"Djenabou all9first/105second old joint profiles preserved, equal independently counted finite original histories");
        bool menusOk=true;int menus=0,menuAnswers=0;
        foreach(Dictionary<string,object> sr in (IEnumerable)reference["scenes"]) {
            string name=(string)sr["scene"];var row=(Dictionary<string,object>)rows[name];var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
            foreach(Dictionary<string,object> m in (IEnumerable)sr["menus"]) {
                menus++;var texts=Strings(m["texts"]);var links=Strings(m["links"]);var current=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)m["passage"]]["_choices"])current.Add(c);
                menusOk &= current.Count==texts.Count&&texts.Count==links.Count;
                for(int i=0;i<current.Count;i++){menuAnswers++;menusOk &= (string)current[i]["_text"]==texts[i]&&(string)current[i]["_link"]==links[i];}
            }
            var seen=new HashSet<string>();var answers=new HashSet<string>();int rides=0;bool first=name.EndsWith("01");
            foreach(Dictionary<string,object> r in (IEnumerable)sr["routes"]) {
                List<string> text,actions;var story=ReferenceRun(row,r,(string)sr["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,answers);string all=string.Join("\n",text.ToArray());var path=Strings(r["passages"]);
                bool ok=!story.can_continue&&story.GetChoices().Count==0&&!actions.Exists(x=>x.Contains("clue:"));
                if(first) {
                    ok &= all.Contains("Я проснулась")&&all.Contains("песня в стиле рай")&&all.Contains("Сегодня ночью я убила пациентку")&&!all.Contains("убила пациента")&&all.Contains("усиливаете обогрев");
                    if(path.Contains("djena-01-hot"))ok &= all.Contains("пассажирка крепко спит");
                    if(path.Contains("djena-01-later-noproblem"))ok &= all.Contains("Она вяло кивает");
                } else {
                    bool good=path.Contains("djena-02-revelation03");ok &= all.Contains("случайно убила пациентку")&&all.Contains("выглядит менее уставшей, более бодрой")&&!all.Contains("не был экстренным");
                    if(path.Contains("djena-02-anger04"))ok &= all.Contains("ПЯТЬ секунд")&&all.Contains("не ЛУНУ с неба прошу")&&all.Contains("глухой, затаённый, ледяной");
                    if(path.Contains("djena-02-drive-heater"))ok &= all.Contains("У нас котельная барахлит")&&all.Contains("изнываю от жары")&&all.Contains("Пациенты жалуются");
                    if(good)ok &= all.Contains("Я жалею об этом")&&all.Contains("Я не жалею об этом")&&all.Contains("Действовать быстрее")&&all.Contains("она просто не была в приоритете")&&all.Contains("Другие…")&&!all.Contains("Другие пациенты");
                }
                Check(ok,"Djenabou complete representative ride "+name+"/"+(++rides)+" exact French text/menu/state/orderedactions; guilt versus medical priority and silence timing remain scoped");
            }
            Check(rides==(first?5:12)&&seen.Count==(first?9:26)&&answers.Count==(first?6:15),"Djenabou complete representative coverage "+name+" all original passages/answers; all finite full histories separately retained");
            var data=Strings(row["_user_datas"]);Check(data.Count==7&&data[0]==(first?"passengercall: «В Монтрёй».":"passengercall: «В Монтрёй, пожалуйста».")&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==6,"Djenabou original useful Montreuil request and6unchanged animation definitions "+name);
        }
        Check(menusOk&&menus==6&&menuAnswers==21,"Djenabou all6source menus/21answer edges retain original icons/French-aligned text");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2369.json");Check((string)card["fullname"]=="Djenabou Henri"&&Convert.ToInt32(card["age"])==27&&Convert.ToInt32(card["gender"])==0,"Djenabou source female27card and doctor testimony, no medical/legal verdict or inferred patient biography");
    }
    static void FrancineChecks(Dictionary<string,object> rows) {
        string ct;int profiles=0,linked=0;
            for(int frCat=0;frCat<2;frCat++)for(int frIntro=0;frIntro<3;frIntro++)for(int frEarly=0;frEarly<6;frEarly++)
            for(int frTruth=0;frTruth<2;frTruth++)for(int frLast=0;frLast<4;frLast++) {
                bool immediateBad=frCat==0 && frTruth==0;
                if(immediateBad && frLast!=0)continue;
                var fr=FrancineStart(Load((Dictionary<string,object>)rows["041_francine_01"]),frCat);var emitted=new List<string>();
                fr.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(fr);
                fr.ChooseAnswer(frIntro);ct=string.Join("\n",Drain(fr).ToArray());
                bool coherent=HasText(ct,"Его зовут Круки") && HasText(ct,"кот, которого вы встретили той ночью");
                coherent &= HasText(ct,"что она много плакала")== (frIntro==2);
                fr.ChooseAnswer(frEarly<2?0:1);ct=string.Join("\n",Drain(fr).ToArray());
                coherent &= HasText(ct,"Она отшатывается")== (frEarly>=2);
                fr.ChooseAnswer(frEarly<2?frEarly:frEarly-2);ct=string.Join("\n",Drain(fr).ToArray());
                coherent &= fr.FindVariable("tell").value==(frEarly==0?1:0) && fr.FindVariable("_Driver.Crouky").value==frCat;
                coherent &= HasText(ct,"На поезд?")== (frEarly==0) && HasText(ct,"украл мои деньги")== (frEarly==0);
                coherent &= HasText(ct,"Я знаю, что вы с ним разговаривали")== (frEarly<2);
                coherent &= HasText(ct,"Понятия не имею, где он")== (frEarly==1);
                coherent &= HasText(ct,"мне хотелось в это поверить")== (frEarly==2);
                coherent &= !HasText(ct,"я хотел");
                bool history=frEarly!=3 && frEarly!=5;
                coherent &= HasText(ct,"спас мне жизнь")==history && HasText(ct,"сменила фирму")==history;
                bool toy=frEarly==1 || frEarly==2 || frEarly==4;
                coherent &= HasText(ct,"мягкая игрушка")==toy && !HasText(ct,"чучело");
                coherent &= HasText(ct,"Когда умерла моя мама")== (frEarly==0);
                coherent &= HasText(ct,"бездомный внизу у моего дома")== (frEarly!=0);
                fr.ChooseAnswer(frTruth);ct=string.Join("\n",Drain(fr).ToArray());
                coherent &= emitted.Exists(a=>a.Contains("041_01"))==(frTruth==0) && emitted.Exists(a=>a.Contains("041_02"))==(frTruth==1);
                coherent &= HasText(ct,"Я его прогнал")==immediateBad;
                coherent &= HasText(ct,"вокзал Сен-Лазар")== (frTruth==0 && frCat==1);
                coherent &= HasText(ct,"В Довиль. Я знаю")== (frTruth==0 && frCat==1);
                if(!immediateBad){coherent &= fr.GetChoices().Count==4;fr.ChooseAnswer(frLast);ct=string.Join("\n",Drain(fr).ToArray());}
                bool angry=immediateBad || frLast==1;
                bool healthy=!immediateBad && frLast==0;
                coherent &= emitted.Exists(a=>a.Contains("041_03"))==angry;
                coherent &= HasText(ct,"Полицейская надо мной поиздевалась")==angry && HasText(ct,"НЕ КОТ")==angry;
                coherent &= HasText(ct,"Будь то кот, мужчина или женщина")==healthy && HasText(ct,"купюру")==healthy;
                coherent &= HasText(ct,"И это ваша вина")== (!immediateBad && frLast==3);
                coherent &= HasText(ct,"побыть одной")== (!immediateBad && (frLast==0 || frLast==2));
                coherent &= HasText(ct,"леденеют лёгкие") && !fr.can_continue && fr.GetChoices().Count==0;
                var frTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= frTips.Count==((angry || healthy)?1:0);
                if(angry)coherent &= frTips[0].Contains("none");if(healthy)coherent &= frTips[0].Contains("medium");
                coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("start")).Count==0;
                coherent &= emitted.FindAll(a=>a.Contains("travel") && a.Contains("stop")).Count==(immediateBad?1:0);
                coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==0;
                profiles++;
                Check(coherent,"Francine complete route preserves owner/toy/mother, truthful cat history and exact endings "+frCat+"/"+frIntro+"/"+frEarly+"/"+frTruth+"/"+frLast);
            }
            for(int catRoute=0;catRoute<4;catRoute++) {
                var catStory=new PrompterStory(Load((Dictionary<string,object>)rows["040_crouky_01"]),false);
                catStory.OnActionTriggered+=(evt)=>{};
                catStory.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Crouky",0),new DialogVariable("_Driver.Crouky",0)},"crouky-start");Drain(catStory);
                if(catRoute==0 || catRoute==3) {
                    catStory.ChooseAnswer(0);Drain(catStory);catStory.ChooseAnswer(0);Drain(catStory);
                    catStory.ChooseAnswer(catRoute==0?0:1);Drain(catStory);
                    if(catRoute==0){catStory.ChooseAnswer(3);Drain(catStory);catStory.ChooseAnswer(1);Drain(catStory);}
                } else {
                    catStory.ChooseAnswer(1);Drain(catStory);catStory.ChooseAnswer(0);Drain(catStory);catStory.ChooseAnswer(catRoute-1);Drain(catStory);
                }
                int actualCat=catStory.FindVariable("_Driver.Crouky").value;
                bool coherent=!catStory.can_continue && actualCat==(catRoute==0?1:0);
                var follow=FrancineStart(Load((Dictionary<string,object>)rows["041_francine_01"]),actualCat);Drain(follow);
                follow.ChooseAnswer(0);Drain(follow);follow.ChooseAnswer(1);Drain(follow);follow.ChooseAnswer(2);Drain(follow);
                follow.ChooseAnswer(0);ct=string.Join("\n",Drain(follow).ToArray());
                coherent &= HasText(ct,"вокзал Сен-Лазар")== (actualCat==1) && HasText(ct,"Я его прогнал")== (actualCat==0);
                if(actualCat==1){coherent &= follow.GetChoices().Count==4;follow.ChooseAnswer(0);Drain(follow);}
                coherent &= !follow.can_continue && follow.GetChoices().Count==0;
                linked++;
                Check(coherent,"Crouky actual previous trip/refusal/wait/leave state selects Francine's corresponding late truth "+catRoute);
            }

        var reference=ReferenceFile("localization/francine_fr_validation_routes.json");var row=(Dictionary<string,object>)rows["041_francine_01"];
        var counts=new List<int>();foreach(object value in (IEnumerable)reference["full_answer_histories_by_profile"])counts.Add(Convert.ToInt32(value));
        Check(profiles==234&&linked==4&&counts.Count==2&&counts[0]==90&&counts[1]==144,"Francine all234old joint profiles equal independent90+144finite histories; all4actual040trip/refusal/wait/leave handoffs retained");
        var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        bool menuOk=true;int menus=0,menuAnswers=0;
        foreach(Dictionary<string,object> m in (IEnumerable)reference["menus"]) {
            menus++;var texts=Strings(m["texts"]);var links=Strings(m["links"]);var current=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)m["passage"]]["_choices"])current.Add(c);
            menuOk &= current.Count==texts.Count&&texts.Count==links.Count;
            for(int i=0;i<current.Count;i++){menuAnswers++;menuOk &= (string)current[i]["_text"]==texts[i]&&(string)current[i]["_link"]==links[i];}
        }
        Check(menuOk&&menus==6&&menuAnswers==17,"Francine all6source menus/17answer edges retain original icons/French-aligned text");
        var seen=new HashSet<string>();var answers=new HashSet<string>();int rides=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,answers);string all=string.Join("\n",text.ToArray());var path=Strings(r["passages"]);
            bool ok=!story.can_continue&&story.GetChoices().Count==0&&all.Contains("тихим, неуверенным голосом")&&all.Contains("переводит дыхание")&&all.Contains("леденеют лёгкие")&&!actions.Exists(x=>x.Contains("clue:"))&&!actions.Exists(x=>x.Contains("travel: start"));
            if(path.Contains("francine-end-bad02"))ok &= all.Contains("Никто не хочет понять")&&all.Contains("Она кивает. Голос надломился")&&all.Contains("не умеет о себе позаботиться");
            if(path.Contains("francine-end01"))ok &= all.Contains("может, я душила его своей заботой");
            if(path.Contains("francine-end01-healthy"))ok &= all.Contains("душишь своей любовью")&&all.Contains("Будь то кот, мужчина или женщина");
            if(path.Contains("francine-end01-moveon"))ok &= all.Contains("ФРАНСИН: «Жить дальше?»");
            if(path.Contains("francine-pleasetellme"))ok &= all.Contains("Когда умерла моя мама")&&all.Contains("это поможет отвлечься");
            Check(ok,"Francine complete representative ride "+(++rides)+" exact French text/menu/state/orderedactions, suffocating care metaphor and uncertain cat death remain scoped");
        }
        Check(rides==32&&seen.Count==32&&answers.Count==17,"Francine32representatives cover32passages127lines17answers; all234finite histories and4actual040handoffs separately retained");
        var data=Strings(row["_user_datas"]);Check(data.Count==6&&data[0]=="passengercall: «Мне нужно с вами поговорить…»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==5,"Francine original useful request and5animation definitions retained, no new icon drawings");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2368.json");Check((string)card["fullname"]=="Francine Gayard"&&Convert.ToInt32(card["age"])==27&&Convert.ToInt32(card["gender"])==0,"Francine source female27card, mother dead by testimony; cat death/unnamed police identity not established");
    }
    static void ShinjiChecks(Dictionary<string,object> rows) {
        string ct;int profiles=0;
            for(int shFirst=0;shFirst<3;shFirst++)for(int shEiffel=0;shEiffel<4;shEiffel++)for(int shAnger=0;shAnger<4;shAnger++)
            for(int shParis=0;shParis<2;shParis++)for(int shVersailles=0;shVersailles<3;shVersailles++)for(int shAsk=0;shAsk<2;shAsk++)
            for(int shArrival=0;shArrival<2;shArrival++)for(int shOutside=0;shOutside<4;shOutside++)for(int shExit=0;shExit<3;shExit++) {
                if(shFirst==2 && shEiffel!=0)continue;
                if(shAsk==1 && shExit!=0)continue;
                var sh=ShinjiStart(Load((Dictionary<string,object>)rows["037_shinji_01"]));var emitted=new List<string>();
                sh.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(sh).ToArray());
                bool coherent=sh.FindVariable("_Driver.Met.Shinji").value==1 && HasText(ct,"сорока пяти") && HasText(ct,"Шарль-де-Голль");
                coherent &= emitted.Exists(a=>a.Contains("037_00")) && HasText(ct,"Японец? Китаец? Вы не знаете");
                sh.ChooseAnswer(shFirst);ct=string.Join("\n",Drain(sh).ToArray());
                coherent &= HasText(ct,"Он щурится")== (shFirst==0);
                if(shFirst!=2) {
                    coherent &= HasText(ct,"Пассажир морщится") && HasText(ct,"おぞましい。とても醜いよ。");
                    sh.ChooseAnswer(shEiffel);ct=string.Join("\n",Drain(sh).ToArray());
                    coherent &= HasText(ct,"английские слова")== (shEiffel==2);
                }
                coherent &= HasText(ct,"なんて言っているんだ") && HasText(ct,"Так, это японский. Теперь вы уверены");
                sh.ChooseAnswer(shAnger);ct=string.Join("\n",Drain(sh).ToArray());
                coherent &= HasText(ct,"そう！ そのとおり！ 空港だ！")== (shAnger==1);
                coherent &= HasText(ct,"не это он хотел услышать")== (shAnger==0 || shAnger==2);
                coherent &= HasText(ct,"大阪に戻るのが待ち遠しいよ。");
                sh.ChooseAnswer(shParis);ct=string.Join("\n",Drain(sh).ToArray());
                coherent &= HasText(ct,"не обращает на вас внимания")== (shParis==0);
                coherent &= HasText(ct,"ああ、食べ物も最悪だ！") && HasText(ct,"В третий раз") && HasText(ct,"ベルサイユは良かったが");
                sh.ChooseAnswer(shVersailles==0?1:0);ct=string.Join("\n",Drain(sh).ToArray());
                if(shVersailles!=0) {
                    coherent &= HasText(ct,"共産党か？") && HasText(ct,"Вы не понимаете");
                    sh.ChooseAnswer(shVersailles==1?1:0);ct=string.Join("\n",Drain(sh).ToArray());
                }
                coherent &= HasText(ct,"パリは、みにくい？")== (shVersailles==2);
                coherent &= HasText(ct,"Токио") && HasText(ct,"не хочет с вами разговаривать");
                sh.ChooseAnswer(shAsk);ct=string.Join("\n",Drain(sh).ToArray());
                coherent &= sh.FindVariable("yakuza").value==(shAsk==0?1:0);
                coherent &= HasText(ct,"Якудза! Вот!")== (shAsk==0);
                coherent &= HasText(ct,"私がヤクザだって？")== (shAsk==0);
                coherent &= HasText(ct,"фильм, который посмотрели в больнице") && HasText(ct,"название не вспоминается");
                coherent &= HasText(ct,"родители читали по-арабски и писали по-французски") && HasText(ct,"через съезды и туннели");
                coherent &= HasText(ct,"す… すまなかった。");
                sh.ChooseAnswer(shArrival);ct=string.Join("\n",Drain(sh).ToArray());
                coherent &= HasText(ct,"СИНДЗИ: « ありがとう。 »") && HasText(ct,"пристЫженным") && HasText(ct,"Весит целую тонну");
                sh.ChooseAnswer(shOutside);ct=string.Join("\n",Drain(sh).ToArray());
                coherent &= HasText(ct,"тон понял")== (shOutside==2);
                coherent &= HasText(ct,"оглядывает вас")== (shOutside!=2);
                coherent &= HasText(ct,"クリスマスに息子が亡くなったんだ。") && HasText(ct,"白血病でね。19歳だった。");
                coherent &= HasText(ct,"Может, жену?") && HasText(ct,"Если бы вы только могли его понять") && HasText(ct,"его глаза блестят") && HasText(ct,"Не за что");
                coherent &= HasText(ct,"у самых дверей")== (shAsk==0);
                if(shAsk==0){coherent &= sh.GetChoices().Count==3;sh.ChooseAnswer(shExit);ct=string.Join("\n",Drain(sh).ToArray());}
                coherent &= !sh.can_continue && sh.GetChoices().Count==0;
                coherent &= HasText(ct,"просыпается аэропорт") && HasText(ct,"замёрзшие руки");
                var shTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= shTips.Count==1 && shTips[0].Contains("medium");
                coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==0;
                profiles++;
                Check(coherent,"Shinji complete language-barrier route preserves foreign speech, loss/guess distinction and yakuza callback "+shFirst+"/"+shEiffel+"/"+shAnger+"/"+shParis+"/"+shVersailles+"/"+shAsk+"/"+shArrival+"/"+shOutside+"/"+shExit);
            }

        var reference=ReferenceFile("localization/shinji_fr_validation_routes.json");var row=(Dictionary<string,object>)rows["037_shinji_01"];
        Check(profiles==6912&&Convert.ToInt32(reference["full_answer_histories"])==6912,"Shinji all6912old full joint profiles retained, equal independently counted finite original answer histories");
        var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        bool menuOk=true;int menus=0,menuAnswers=0;
        foreach(Dictionary<string,object> m in (IEnumerable)reference["menus"]) {
            menus++;var texts=Strings(m["texts"]);var links=Strings(m["links"]);var current=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)m["passage"]]["_choices"])current.Add(c);
            menuOk &= current.Count==texts.Count&&texts.Count==links.Count;
            for(int i=0;i<current.Count;i++){menuAnswers++;menuOk &= (string)current[i]["_text"]==texts[i]&&(string)current[i]["_link"]==links[i];}
        }
        Check(menuOk&&menus==10&&menuAnswers==28,"Shinji all10source menus/28answer edges retain original carrier icons and French-aligned text");
        var seen=new HashSet<string>();var answers=new HashSet<string>();int rides=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,answers);string all=string.Join("\n",text.ToArray());var path=Strings(r["passages"]);bool yakuza=path.Contains("shinji-01-yakuza");
            bool ok=!story.can_continue&&story.GetChoices().Count==0&&all.Contains("наверное, турист")&&all.Contains("монотонно, без малейших эмоций")&&all.Contains("Может, жену? Кажется, он взволнован")&&all.Contains("его глаза блестят")&&!all.Contains("со слезами в глазах")&&all.Contains("под обогревателем");
            ok &= all.Contains("クリスマスに息子が亡くなったんだ。")&&all.Contains("白血病でね。19歳だった。")&&all.Contains("Если бы вы только могли его понять")&&!all.Contains("лейкемии");
            if(path.Contains("shinji-01-eiffel"))ok &= all.Contains("Пассажир морщится");
            ok &= story.FindVariable("yakuza").value==(yakuza?1:0)&&path.Contains("shinji-01-exit-yakuza")==yakuza;
            var tips=actions.FindAll(x=>x.Contains("tip:"));ok &= tips.Count==1&&tips[0].Contains("medium")&&!actions.Exists(x=>x.Contains("clue:"));
            Check(ok,"Shinji complete representative ride "+(++rides)+" exact French text/menu/state/orderedactions; Japanese barrier and driver loss guess remain distinct");
        }
        Check(rides==23&&seen.Count==35&&answers.Count==28,"Shinji23representatives cover35passages178lines28answers; all6912finite full histories separately retained");
        var data=Strings(row["_user_datas"]);Check(data.Count==5&&data[0]=="passengercall: «В аэропорт»."&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==4,"Shinji original French airport request and all4animation definitions retained; no icon drawings added");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2366.json");Check((string)card["fullname"]=="Shinji Kurumada"&&Convert.ToInt32(card["age"])==43&&Convert.ToInt32(card["gender"])==3,"Shinji source male43card, not nationality by appearance or driver knowledge of Japanese son story");
    }
    static void SeanChecks(Dictionary<string,object> rows) {
        string ct;int profiles=0;
            for(int sePolice=0;sePolice<3;sePolice++)for(int seOrigin=0;seOrigin<3;seOrigin++)for(int seApology=0;seApology<3;seApology++)
            for(int seLearning=0;seLearning<5;seLearning++)for(int sePhilosophy=0;sePhilosophy<4;sePhilosophy++)for(int seBeauty=0;seBeauty<2;seBeauty++) {
                if(seLearning<2 && sePhilosophy!=0)continue;
                var se=SeanStart(Load((Dictionary<string,object>)rows["036_sean_01"]));var emitted=new List<string>();
                se.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(se);
                bool coherent=se.FindVariable("_Driver.Met.Sean").value==1;
                se.ChooseAnswer(sePolice);ct=string.Join("\n",Drain(se).ToArray());
                coherent &= emitted.Exists(a=>a.Contains("036_01"))==(sePolice==0);
                coherent &= HasText(ct,"racaille")== (sePolice!=2);
                se.ChooseAnswer(seOrigin);ct=string.Join("\n",Drain(se).ToArray());coherent &= HasText(ct,"Чикаго, Иллинойс… США")== (seOrigin==0);
                se.ChooseAnswer(seApology);ct=string.Join("\n",Drain(se).ToArray());
                coherent &= HasText(ct,"Не благодарите меня")== (seApology==1);
                coherent &= HasText(ct,"деньги, телефон") && HasText(ct,"банковская карта и документы") && HasText(ct,"восемь часов");
                se.ChooseAnswer(seLearning==0?1:0);ct=string.Join("\n",Drain(se).ToArray());
                coherent &= HasText(ct,"учусь в школе мимов")== (seLearning!=0);
                if(seLearning!=0) {
                    coherent &= HasText(ct,"Ну же. Скажите") && HasText(ct,"Технику… постановку") && HasText(ct,"Грим");
                    se.ChooseAnswer(seLearning==1?3:seLearning-2);ct=string.Join("\n",Drain(se).ToArray());
                    coherent &= HasText(ct,"Абсолютно")== (seLearning>=3);
                }
                bool talks=seLearning>=2 && sePhilosophy!=3;
                if(seLearning>=2){coherent &= HasText(ct,"Все таксисты такие философы");se.ChooseAnswer(sePhilosophy);ct=string.Join("\n",Drain(se).ToArray());}
                coherent &= se.FindVariable("talk").value==(talks?1:0);
                coherent &= HasText(ct,"Мне полегчало от разговора с тобой")==talks;
                coherent &= HasText(ct,"Он перешёл на «ты»")==talks;
                coherent &= HasText(ct,"американское посольство")==talks;
                coherent &= HasText(ct,"Вы улыбаетесь и качаете головой")==talks;
                coherent &= HasText(ct,"ему ни к чему это знать")==talks;
                se.ChooseAnswer(seBeauty);ct=string.Join("\n",Drain(se).ToArray());
                coherent &= HasText(ct,"Без сомнений")== (seBeauty==0);
                coherent &= HasText(ct,"протягивает руку")==talks && emitted.Exists(a=>a.Contains("036_02"))==talks;
                coherent &= HasText(ct,"ночные смены")==talks && HasText(ct,"нарастающую тревогу");
                var seTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= seTips.Count==(talks?1:0);
                if(talks)coherent &= seTips[0].Contains("high");
                coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==0;
                profiles++;
                Check(coherent,"Sean complete route retains foreign slang, formal/informal switch and exact gratitude condition "+sePolice+"/"+seOrigin+"/"+seApology+"/"+seLearning+"/"+sePhilosophy+"/"+seBeauty);
            }

        var reference=ReferenceFile("localization/sean_fr_validation_routes.json");var row=(Dictionary<string,object>)rows["036_sean_01"];
        Check(profiles==756&&Convert.ToInt32(reference["full_answer_histories"])==756,"Sean all756old full profiles retained, equal independently counted finite original answer histories");
        var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        bool menuOk=true;int menus=0,menuAnswers=0;
        foreach(Dictionary<string,object> m in (IEnumerable)reference["menus"]) {
            menus++;var texts=Strings(m["texts"]);var links=Strings(m["links"]);var current=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)m["passage"]]["_choices"])current.Add(c);
            menuOk &= current.Count==texts.Count&&texts.Count==links.Count;
            for(int i=0;i<current.Count;i++){menuAnswers++;menuOk &= (string)current[i]["_text"]==texts[i]&&(string)current[i]["_link"]==links[i];}
        }
        Check(menuOk&&menus==7&&menuAnswers==21,"Sean all7original source menus/21answer edges with French-aligned text and original icons");
        var seen=new HashSet<string>();var answers=new HashSet<string>();int rides=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,answers);string all=string.Join("\n",text.ToArray());
            var path=Strings(r["passages"]);bool talked=path.Contains("sean-01-thanks");
            bool ok=!story.can_continue&&story.GetChoices().Count==0&&all.Contains("банковская карта и документы")&&!all.Contains("кредитка")&&all.Contains("Мне говорили")&&all.Contains("И никак иначе")&&all.Contains("Вы продолжаете путь");
            if(path.Contains("sean-01-question-situation"))ok &= all.Contains("racaille — «шпана»")&&all.Contains("для него новое. Вы киваете");
            if(path.Contains("sean-01-hub01-from"))ok &= all.Contains("Чикаго, Иллинойс… США");
            ok &= all.Contains("Мне полегчало от разговора с тобой")==talked&&all.Contains("ему ни к чему это знать")==talked;
            if(talked)ok &= all.Contains("Вы улыбаетесь и качаете головой")&&all.Contains("Он перешёл на «ты»")&&all.Contains("свои ночные смены");
            ok &= story.FindVariable("talk").value==(talked?1:0)&&actions.FindAll(x=>x.Contains("tip:")).Count==(talked?1:0)&&actions.Exists(x=>x.Contains("036_02"))==talked;
            Check(ok,"Sean complete representative ride "+(++rides)+" exact French text/menu/state/orderedactions; police testimony, slang and accidental tu remain scoped");
        }
        Check(rides==16&&seen.Count==31&&answers.Count==21,"Sean16representative rides cover31passages140lines21answers; all756finite histories separately retained");
        var data=Strings(row["_user_datas"]);Check(data.Count==5&&data[0]=="passengercall: «Мне нужно… Домой…»"&&data.Contains("anim-link: C0LD_2_DRIVER -> LOOKING_DRIVER, IDLE_DRIVER")&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==4,"Sean actual French call hesitation and all4original animations retained, C0LD contains original zero");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2365.json");Check((string)card["fullname"]=="Sean Christie"&&Convert.ToInt32(card["age"])==22&&Convert.ToInt32(card["gender"])==3&&Convert.ToInt32(card["ethnicity"])==0,"Sean source male22Christie; african internal enum does not establish passport; Janet Jackson surname discrepancy remains documented");
    }
    static void SylvieChecks(Dictionary<string,object> rows) {
        string ct;int profiles=0;
            for(int sylStart=0;sylStart<2;sylStart++)for(int sylSmile=0;sylSmile<2;sylSmile++)for(int sylAnswer=0;sylAnswer<3;sylAnswer++) {
                var sy=SylvieStart(Load((Dictionary<string,object>)rows["035_sylvainsylvie_01"]));var emitted=new List<string>();
                sy.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(sy).ToArray());
                bool coherent=sy.FindVariable("_Driver.Met.Sylvie").value==1 && HasText(ct,"мать и сын");
                sy.ChooseAnswer(sylStart);ct=string.Join("\n",Drain(sy).ToArray());coherent &= HasText(ct,"Любимая опера моего сына")== (sylStart==0);
                coherent &= HasText(ct,"Я тебя люблю") && HasText(ct,"Знаю, мам");
                sy.ChooseAnswer(sylSmile);ct=string.Join("\n",Drain(sy).ToArray());
                coherent &= HasText(ct,"Цветочный дуэт")== (sylSmile==1);
                coherent &= HasText(ct,"ни красивым, ни умным") && HasText(ct,"блондин — значит честный") && !HasText(ct,"блондинка");
                coherent &= HasText(ct,"Я сам с ним поговорю, когда захочу") && HasText(ct,"Скорее уж мне полегчало");
                coherent &= HasText(ct,"Он ничего не имеет против гомосексуалов") && HasText(ct,"потому что он не поймёт");
                sy.ChooseAnswer(sylAnswer);ct=string.Join("\n",Drain(sy).ToArray());
                coherent &= HasText(ct,"Он ещё ребёнок")== (sylAnswer!=2) && emitted.Exists(a=>a.Contains("035_01"))==(sylAnswer!=2);
                coherent &= HasText(ct,"Ключи ведь у тебя") && HasText(ct,"исчезают в подъезде");
                coherent &= emitted.FindAll(a=>a.Contains("tip")).Count==1 && emitted.Exists(a=>a.Contains("tip") && a.Contains("medium"));
                coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==0;
                profiles++;
                Check(coherent,"Sylvie and Sylvain retain mother/teen son, masculine examples and optional private apology "+sylStart+"/"+sylSmile+"/"+sylAnswer);
            }

        var reference=ReferenceFile("localization/sylvie_fr_validation_routes.json");var row=(Dictionary<string,object>)rows["035_sylvainsylvie_01"];
        Check(profiles==12&&Convert.ToInt32(reference["full_answer_histories"])==12,"Sylvie all12old full profiles retained, equal independently counted finite original answer histories");
        var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        bool menuOk=true;int menus=0,menuAnswers=0;
        foreach(Dictionary<string,object> m in (IEnumerable)reference["menus"]) {
            menus++;var texts=Strings(m["texts"]);var links=Strings(m["links"]);var current=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)m["passage"]]["_choices"])current.Add(c);
            menuOk &= current.Count==texts.Count&&texts.Count==links.Count;
            for(int i=0;i<current.Count;i++){menuAnswers++;menuOk &= (string)current[i]["_text"]==texts[i]&&(string)current[i]["_link"]==links[i];}
        }
        Check(menuOk&&menus==3&&menuAnswers==7,"Sylvie all3immutable source menus/7answer edges with French-aligned text and original icons");
        var seen=new HashSet<string>();var answers=new HashSet<string>();int rides=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,answers);string all=string.Join("\n",text.ToArray());
            bool ok=!story.can_continue&&story.GetChoices().Count==0&&all.Contains("вероятно, мать и сын")&&all.Contains("Опера не по его вкусу")&&!all.Contains("чашка чая");
            ok &= all.Contains("я на твоей стороне")&&all.Contains("Наслаждаюсь этим моментом")&&all.Contains("Скорее уж мне полегчало");
            if(Strings(r["passages"]).Contains("sylsyl-end-problem"))ok &= all.Contains("Она вдруг спохватывается");
            Check(ok,"Sylvie full representative ride "+(++rides)+" exact French text/menu/state/ordered commands; mother support and son relief remain distinct");
        }
        Check(rides==5&&seen.Count==12&&answers.Count==7,"Sylvie5representative rides cover12passages107lines7answers; all12finite full histories separately retained");
        var data=Strings(row["_user_datas"]);Check(data.Count==11&&data[0]=="passengercall: «Нам в Ле-Пре-Сен-Жерве, сразу за Ла-Виллет.»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==10,"Sylvie French request removes English-only hesitation, original prefix and ten animations preserved");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2364.json");Check((string)card["fullname"]=="Sylvie & Sylvain Patrux"&&Convert.ToInt32(card["age"])==44&&Convert.ToInt32(card["gender"])==4,"Sylvie mixed source groupcard44 does not give both ages; mother and teen son established by words, father absent");
    }
    static void PhilChecks(Dictionary<string,object> rows) {
        string ct;int[] profileCounts=new int[4];
            for(int phCase=0;phCase<4;phCase++)for(int phStart=0;phStart<3;phStart++)for(int phError=0;phError<3;phError++)
            for(int phStage=0;phStage<4;phStage++)for(int phAnswer=0;phAnswer<5;phAnswer++) {
                var ph=CarolinaStart(Load((Dictionary<string,object>)rows["034_phil_01"]),"phil-start",phCase,0);
                var emitted=new List<string>();ph.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(ph).ToArray());
                bool coherent=HasText(ct,"аэропорт Руасси") && HasText(ct,"свежесть дыхания");
                var phClues=emitted.FindAll(a=>a.Contains("clue:"));coherent &= phClues.Count==(phCase<3?1:0);
                if(phCase<3) {
                    string[] prefix={"clue_judge_customer_rumor_","clue_sand_customer_rumor_","clue_angel_customer_rumor_"};
                    coherent &= Regex.Matches(phClues[0],prefix[phCase]).Count==(phCase==2?14:10);
                }
                coherent &= ph.FindVariable("_Found.Clue.Today").value==(phCase<3?1:0);
                ph.ChooseAnswer(phStart);Drain(ph);ph.ChooseAnswer(phError);ct=string.Join("\n",Drain(ph).ToArray());
                coherent &= HasText(ct,"олень не пишет книгу о том, как на него охотиться")== (phError==0);
                coherent &= HasText(ct,"пахнет") && HasText(ct,"женщин постарше");
                var monologue=new StringBuilder();
                for(int step=0;step<3;step++) {
                    ph.ChooseAnswer(phStage==step?0:1);ct=string.Join("\n",Drain(ph).ToArray());monologue.Append(ct);
                    if(phStage==step)break;
                }
                coherent &= HasText(monologue.ToString(),"один общий родитель")== (phStage>=1);
                coherent &= HasText(monologue.ToString(),"двадцатью, а то и тридцатью номерами")== (phStage>=2);
                coherent &= HasText(monologue.ToString(),"лучше Руасси")== (phStage==3);
                coherent &= ph.FindVariable("silence").value==(phStart==2?1:0)+(phError==2?1:0)+Math.Min(phStage,2);
                if(phAnswer==0){ph.ChooseAnswer(1);ct=string.Join("\n",Drain(ph).ToArray());coherent &= HasText(ct,"Сначала тем, у кого три звезды") && HasText(ct,"На следующий день — четыре звезды") && HasText(ct,"Пять звёзд. Ещё через день");}
                else if(phAnswer==4){ph.ChooseAnswer(2);ct=string.Join("\n",Drain(ph).ToArray());coherent &= HasText(ct,"Нельзя так обращаться с людьми");}
                else {
                    ph.ChooseAnswer(0);ct=string.Join("\n",Drain(ph).ToArray());coherent &= HasText(ct,"свиданий хватает на") && HasText(ct,"серьёзного ничего не будет");
                    ph.ChooseAnswer(phAnswer==1?1:(phAnswer==2?2:0));ct=string.Join("\n",Drain(ph).ToArray());
                    coherent &= HasText(ct,"и психолог, и таксист")== (phAnswer==3);
                }
                bool aggressive=phAnswer>=3;int finalSilence=(phStart==2?1:0)+(phError==2?1:0)+Math.Min(phStage,2)+(aggressive?-2:1);
                coherent &= ph.FindVariable("silence").value==finalSilence;
                coherent &= emitted.Exists(a=>a.Contains("034_01"))==aggressive && emitted.Exists(a=>a.Contains("034_02"))==(finalSilence>3);
                var phTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= phTips.Count==1 && phTips[0].Contains(aggressive?"none":"medium");
                coherent &= HasText(ct,"плохую оценку")==aggressive;
                coherent &= HasText(ct,"На подошве правой туфли") && HasText(ct,"тёмно-коричневое пятно") && HasText(ct,"заднее стекло");
                coherent &= emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*start")).Count==1 && emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*stop")).Count==1;
                profileCounts[phCase]++;
                Check(coherent,"Phil complete route retains three cases, four interruption points, confrontations and strict silence reveal "+phCase+"/"+phStart+"/"+phError+"/"+phStage+"/"+phAnswer);
            }

        var reference=ReferenceFile("localization/phil_fr_validation_routes.json");var row=(Dictionary<string,object>)rows["034_phil_01"];
        var blocks=new Dictionary<string,Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"])blocks[(string)p["_title"]]=p;
        var independentCounts=new List<int>();foreach(object x in (IEnumerable)reference["full_answer_histories_by_profile"])independentCounts.Add(Convert.ToInt32(x));
        bool complete=independentCounts.Count==4;for(int i=0;i<4;i++)complete &= profileCounts[i]==180&&independentCounts[i]==profileCounts[i];
        Check(complete,"Phil all720old complete joint profiles retained; independent original graph counts180finite answer histories per case/no-case profile");
        int menus=0,sourceAnswers=0;bool menuOk=true;
        foreach(Dictionary<string,object> m in (IEnumerable)reference["menus"]) {
            menus++;var texts=Strings(m["texts"]);var links=Strings(m["links"]);var current=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)blocks[(string)m["passage"]]["_choices"])current.Add(c);
            menuOk &= current.Count==texts.Count&&texts.Count==links.Count;
            for(int i=0;i<current.Count;i++){sourceAnswers++;menuOk &= (string)current[i]["_text"]==texts[i]&&(string)current[i]["_link"]==links[i];}
        }
        Check(menuOk&&menus==7&&sourceAnswers==18,"Phil seven immutable-source menus/eighteen answer targets with manually read French-aligned text and original choice icons");
        var seen=new HashSet<string>();var answers=new HashSet<string>();int rides=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,answers);var path=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",text.ToArray());
            bool correct=!story.can_continue&&story.GetChoices().Count==0&&all.Contains("аэропорт Руасси")&&all.Contains("На подошве правой туфли");
            if(path.Contains("phil-airport2"))correct &= all.Contains("сестра только наполовину")&&all.Contains("один общий родитель")&&!all.Contains("сводная сестра");
            if(path.Contains("phil-airport4"))correct &= all.Contains("Он хлопает в ладоши")&&!all.Contains("Он потирает руки");
            if(path.Contains("phil-question-after"))correct &= all.Contains("Я оцениваю женщин")&&all.Contains("три звезды")&&all.Contains("четыре звезды")&&all.Contains("пять звёзд. Ещё через день");
            if(path.Contains("phil-question-really"))correct &= all.Contains("В этом меньше")&&all.Contains("Личного")&&all.Contains("свиданий хватает на");
            if(path.Contains("phil-fearwomen"))correct &= all.Contains("он нервничает")&&all.Contains("и психолог, и таксист");
            Check(correct,"Phil full original representative ride "+(++rides)+" exact text/menu/state/ordered actions and French family/clap/grades/uncertainty");
        }
        Check(rides==144&&seen.Count==30&&answers.Count==18,"Phil144complete representative rides cover30blocks125lines18answers across88menu states; equivalent prefixes merged, all720old joint profiles separately retained");
        var data=Strings(row["_user_datas"]);
        Check(data.Count==6&&data[0]=="passengercall: «Шарль-де-Голль, пошустрее!»"&&data.FindAll(x=>x.StartsWith("anim-link:")).Count==5,"Phil original French CharlesdeGaulle invitation and five untouched animation definitions; Roissy in body is same airport");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2363.json");
        Check((string)card["fullname"]=="Phil Bourgeois"&&Convert.ToInt32(card["age"])==23&&Convert.ToInt32(card["gender"])==3,"Phil original male23card; model appearance not established profession, sister story not confirmed real family");
    }
    static void CarloChecks(Dictionary<string,object> rows) {
        string ct;
            int[] carloPhoneScores={3,1,0},carloEncounterScores={1,1,3,1,3},carloDelayScores={1,3,2,1};
            string[] carloPhoneRecaps={"крадут телефон, который он оставил","роняет телефон в унитаз","телефон совсем разряжен"};
            string[] carloEncounterRecaps={"Никола Панзи","Матильда Эйприл","Даниэль"};
            string[] carloDelayRecaps={"по дороге видит","таинственная фигура","Сюжет захватывающий"};
            string[] carloRatings={"идеальную ложь","совсем неплохо","не лучшая наша работа"};
            for(int phone=0;phone<3;phone++)for(int encounter=0;encounter<5;encounter++)for(int delay=0;delay<4;delay++) {
                var c=Load((Dictionary<string,object>)rows["021_carlo_01"]);var cs=Start(c,"carlo-01lie");var emitted=new List<string>();
                cs.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(cs);cs.ChooseAnswer(phone);Drain(cs);
                cs.ChooseAnswer(encounter<2?encounter:2);Drain(cs);
                if(encounter>=2){cs.ChooseAnswer(encounter-2);Drain(cs);}
                cs.ChooseAnswer(delay);Drain(cs);cs.ChooseAnswer(1);ct=string.Join("\n",Drain(cs).ToArray());
                int score=carloPhoneScores[phone]+carloEncounterScores[encounter]+carloDelayScores[delay];
                bool coherent=cs.FindVariable("carlo-lie").value==score;
                for(int other=0;other<3;other++)coherent &= ct.Contains(carloPhoneRecaps[other])==(phone==other);
                int selectedEncounter=encounter<2?encounter:2,selectedDelay=delay==3?0:delay;
                for(int other=0;other<3;other++)coherent &= ct.Contains(carloEncounterRecaps[other])==(selectedEncounter==other);
                for(int other=0;other<3;other++)coherent &= ct.Contains(carloDelayRecaps[other])==(selectedDelay==other);
                coherent &= ct.Contains("пчеловодителем")== (encounter==3);
                coherent &= ct.Contains("пчеловодом")== (encounter==2 || encounter==4);
                int rating=score==9?0:(score>=5?1:2);
                for(int other=0;other<3;other++)coherent &= ct.Contains(carloRatings[other])==(rating==other);
                coherent &= emitted.Exists(a=>a.Contains("021_03"))==(score==9);
                Check(coherent,"Carlo three selected excuses, profession, score and perfect-lie reveal agree "+phone+"/"+encounter+"/"+delay+" score="+score);
            }
            for(int idea=0;idea<3;idea++) {
                var c=Load((Dictionary<string,object>)rows["021_carlo_01"]);var cs=Start(c,"carlo-happy2");Drain(cs);cs.ChooseAnswer(idea);
                ct=string.Join("\n",Drain(cs).ToArray());
                Check(cs.GetChoices().Count==3 && cs.GetChoices()[0].GetText().Contains("Украли") &&
                    (idea==0?ct.Contains("ПОСЛЕ того, как начал пить"):(idea==1?ct.Contains("ДО того"):ct.Contains("среди дня"))),
                    "Carlo three phone explanations converge while retaining actual versus invented timing "+idea);
            }
            for(int wake=0;wake<2;wake++) {
                var c=Load((Dictionary<string,object>)rows["021_carlo_01"]);var cs=Start(c,"carlo-help");Drain(cs);cs.ChooseAnswer(1);Drain(cs);
                cs.ChooseAnswer(0);Drain(cs);cs.ChooseAnswer(wake);ct=string.Join("\n",Drain(cs).ToArray());
                Check(ct.Contains("Пять лет в вузе") && ct.Contains("разрешениями на съёмки"),"Carlo initial refusal can be reversed and both wake choices preserve job "+wake);
            }
            for(int response=0;response<3;response++) {
                var c=Load((Dictionary<string,object>)rows["021_carlo_01"]);var cs=Start(c,"carlo-help");var emitted=new List<string>();
                cs.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(cs);cs.ChooseAnswer(1);Drain(cs);cs.ChooseAnswer(1);Drain(cs);
                cs.ChooseAnswer(response);ct=string.Join("\n",Drain(cs).ToArray());
                Check(ct.Contains("…мудак!") && ct.Contains("бумажник") && emitted.Exists(a=>a.Contains("021_02")),
                    "Carlo firm refusal and all moral reactions reach split insult and wallet ending "+response);
            }
            var carloEarly=Load((Dictionary<string,object>)rows["021_carlo_01"]);
            var carloStory=Start(carloEarly,"carlo-03lie-outro");Drain(carloStory);carloStory.ChooseAnswer(0);Drain(carloStory);carloStory.ChooseAnswer(0);
            ct=string.Join("\n",Drain(carloStory).ToArray());
            Check(ct.Contains("Она недавно родила") && ct.Contains("Другие водители"),"Carlo early moral objection reaches refusal ending instead of continuing alibi recap");
            foreach(int lesson in new int[]{0,1}) {
                var c=Load((Dictionary<string,object>)rows["021_carlo_01"]);var cs=Start(c,"carlo-end-good");var emitted=new List<string>();
                cs.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(cs);cs.ChooseAnswer(lesson);ct=string.Join("\n",Drain(cs).ToArray());
                Check(emitted.Exists(a=>a.Contains("tip") && a.Contains(lesson==0?"medium":"high")) &&
                    ct.Contains("теперь тебя это смущает")== (lesson==0),"Carlo late moral objection keeps distinct response and original tip "+lesson);
            }
            foreach(int more in new int[]{0,1}) {
                var c=Load((Dictionary<string,object>)rows["021_carlo_01"]);var cs=Start(c,"carlo-end-bad2");var emitted=new List<string>();
                cs.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(cs);cs.ChooseAnswer(more);ct=string.Join("\n",Drain(cs).ToArray());
                Check(emitted.Exists(a=>a.Contains("tip") && a.Contains(more==0?"medium":"high")) &&
                    ct.Contains("выталкиваете его")== (more==1) && ct.Contains("помогаете выйти")== (more==0),
                    "Carlo wallet amount choice preserves exact-fare medium command and extra-money high command "+more);
            }

        var reference=ReferenceFile("localization/carlo_fr_validation_routes.json");var seen=new HashSet<string>();var answers=new HashSet<string>();int routes=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun((Dictionary<string,object>)rows["021_carlo_01"],r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,answers);var path=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",text.ToArray());
            bool correct=!story.can_continue&&story.GetChoices().Count==0;
            if(path.Contains("carlo-03lie-attack"))correct &= all.Contains("грушевого бренди");
            if(path.Contains("carlo-02lie-friend"))correct &= all.Contains("мёд и пчёл делает")&&all.Contains("экологически чистые")&&!all.Contains("энтомолог")&&!all.Contains("Человек-паук");
            if(path.Contains("carlo-02lie-sport"))correct &= all.Contains("Парижский бомбардир");
            if(path.Contains("carlo-recap-02a"))correct &= all.Contains("после удара головой");
            if(path.Contains("carlo-recap-02-logic"))correct &= all.Contains("Он поднимает три пальца")&&all.Contains("дважды в жизни");
            correct &= all.Contains("Его голос становится ещё тише")&&all.Contains("Он кивает с отвращением");
            Check(correct,"Carlo full immutable-source representative ride "+(++routes)+" exact French-aligned lines/menus/state/orderedactions and distinct constructed-alibi parts");
        }
        Check(routes==315&&seen.Count==72&&answers.Count==40,"Carlo315complete original representative rides cover72blocks392slots40answers207menu states; equivalent prefixes merged, not all Cartesian histories");
        var metadata=Strings(((Dictionary<string,object>)rows["021_carlo_01"])["_user_datas"]);
        Check(metadata.Count==9&&metadata[0]=="passengerall: «Перепил. Домой не добраться»."&&metadata.FindAll(x=>x.StartsWith("anim-link:")).Count==8,"Carlo French help request keeps original misspelled passengerall prefix and eight original animations");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2350.json");
        Check((string)card["fullname"]=="Carlo Cerruti"&&Convert.ToInt32(card["age"])==27&&Convert.ToInt32(card["gender"])==3,"Carlo male27 sourcecard; wife/newborn from actual speech, baby sex not inferred");
    }
    static void SkaterzChecks(Dictionary<string,object> rows) {
        string ct;
            int[][] skaterOrders={new int[]{0,1,2},new int[]{0,2,1},new int[]{1,0,2},new int[]{1,2,0},new int[]{2,0,1},new int[]{2,1,0}};
            string[] skaterLocations={"бар","гостиниц","парк"};int[] skaterDestinationIds={2,1,0};
            string[] skaterEndReveals={"026_03","026_02","026_04"};
            for(int order=0;order<6;order++)for(int info=0;info<3;info++)for(int blue=0;blue<6;blue++) {
                var d=Load((Dictionary<string,object>)rows["026_skaterz_01"]);var us=SkaterzStart(d,"ultra-01-start");var emitted=new List<string>();
                us.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(us);us.ChooseAnswer(info);Drain(us);us.ChooseAnswer(0);var beginning=Drain(us);
                ChooseLabel(us,skaterLocations[skaterOrders[order][0]]);ct=string.Join("\n",Drain(us).ToArray());
                if(us.GetChoices()[0].GetText()=="«Э…»"){us.ChooseAnswer((order+info)%2);ct+="\n"+string.Join("\n",Drain(us).ToArray());}
                bool coherent=us.FindVariable("yellow_found").value==1 && beginning.Exists(t=>t.Contains("Из-под розового шлема слышен вздох")) && beginning.Exists(t=>t.Contains("ПИИВО"));
                ChooseLabel(us,skaterLocations[skaterOrders[order][1]]);ct+="\n"+string.Join("\n",Drain(us).ToArray());
                if(us.GetChoices()[0].GetText()=="«Э…»"){us.ChooseAnswer((order+info+1)%2);ct+="\n"+string.Join("\n",Drain(us).ToArray());}
                coherent &= us.FindVariable("yellow_found").value==2 && us.GetChoices().Count==2;
                us.ChooseAnswer(blue==0?1:0);ct+="\n"+string.Join("\n",Drain(us).ToArray());
                if(blue!=0) {
                    us.ChooseAnswer(blue==1?2:((blue==3 || blue==5)?1:0));ct+="\n"+string.Join("\n",Drain(us).ToArray());
                    if(blue>=2) {
                        us.ChooseAnswer(blue<=3?0:(blue==4?1:2));ct+="\n"+string.Join("\n",Drain(us).ToArray());
                        if(blue<=3){us.ChooseAnswer(blue==2?0:1);ct+="\n"+string.Join("\n",Drain(us).ToArray());}
                    }
                }
                coherent &= us.FindVariable("yellow_found").value==5 && us.GetChoices().Count==3;
                coherent &= us.FindVariable("laughter").value==((blue==2 || blue==4)?1:0);
                coherent &= ct.Contains("ОН ВСЁ ВРЕМЯ БОЛТАЕТ")== (info==0 && blue!=0);
                coherent &= ct.Contains("Для мальчишника")== (blue==2 || blue==3);
                coherent &= ct.Contains("девственником оставаться")== (blue==2 || blue==3);
                coherent &= ct.Contains("Тот начинает стонать")== (blue==4);
                coherent &= ct.Contains("Розовый толкает Синего пальцем")== (blue==4);
                coherent &= ct.Contains("И С ПЕРВОЙ ПОПЫТКИ")== (skaterOrders[order][2]==0);
                coherent &= ct.Contains("ПУФ!")== (info==0 && blue!=0);
                coherent &= !ct.Contains("ВОТ ЭТО МЫ ВЛЯПАЛИСЬ");
                us.ChooseAnswer(info);string tail=string.Join("\n",Drain(us).ToArray());
                coherent &= tail.Contains("СИМВОЛ ЖИЗНИ")== (info==2);
                coherent &= tail.Contains("На мочу не похоже") && tail.Contains("И на рвоту тоже") && tail.Contains("До следующего пассажира высохнет");
                coherent &= us.FindVariable("_Car.Clean").value==2;
                for(int place=0;place<3;place++){string reveal=skaterEndReveals[place];coherent &= emitted.Exists(a=>a.Contains(reveal))==(skaterOrders[order][2]==place);}
                var destinations=new List<int>();
                foreach(string action in emitted)if(action.Contains("destination")){var match=Regex.Match(action,@"[=:]\s*(\d+)");if(match.Success)destinations.Add(int.Parse(match.Groups[1].Value));}
                coherent &= destinations.Count==3;
                if(destinations.Count==3)for(int step=0;step<3;step++)coherent &= destinations[step]==skaterDestinationIds[skaterOrders[order][step]];
                coherent &= emitted.Exists(a=>a.Contains("tip") && a.Contains("high"));
                Check(coherent,"Skaterz six search orders with optional Blue conversation, running jokes, third-place reveal and cleaning "+order+"/"+info+"/"+blue);
            }
            for(int info=0;info<3;info++)for(int response=0;response<2;response++) {
                var d=Load((Dictionary<string,object>)rows["026_skaterz_01"]);var us=SkaterzStart(d,"ultra-01-start");var emitted=new List<string>();
                us.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(us);us.ChooseAnswer(info);Drain(us);us.ChooseAnswer(1);
                ct=string.Join("\n",Drain(us).ToArray());bool coherent=ct.Contains("БЛЕВОТА — СИМВОЛ ЖИЗНИ")== (info==2);
                us.ChooseAnswer(response);ct=string.Join("\n",Drain(us).ToArray());
                coherent &= us.FindVariable("_Car.Clean").value==3;
                if(response==0)coherent &= ct.Contains("Мы из Эври") && emitted.Exists(a=>a.Contains("026_01")) && emitted.Exists(a=>Regex.IsMatch(a,@"fare\s*:\s*0(?:\.0)?"));
                else coherent &= ct.Contains("Были в „Фуриозе“") && us.GetChoices().Count==4 && !emitted.Exists(a=>a.Contains("026_01"));
                Check(coherent,"Skaterz refusal keeps symbol callback, zero-fare exit or conditional agreement "+info+"/"+response);
            }
            foreach(int intro in new int[]{0,1}) {
                var d=Load((Dictionary<string,object>)rows["026_skaterz_01"]);var us=SkaterzStart(d,"ultra-01-intro");Drain(us);us.ChooseAnswer(intro);
                ct=string.Join("\n",Drain(us).ToArray());
                Check(ct.Contains("Эдуару")== (intro==0) && ct.Contains("Мужчина…") && us.GetChoices().Count==3,
                    "Skaterz optional unnamed childhood show preserves delayed Pink gender recognition "+intro);
            }
            foreach(int interrupt in new int[]{0,1}) {
                var d=Load((Dictionary<string,object>)rows["026_skaterz_01"]);var us=SkaterzStart(d,"ultra-01-park-empty");Drain(us);us.ChooseAnswer(interrupt);
                ct=string.Join("\n",Drain(us).ToArray());
                Check(ct.Contains("0286")== (interrupt==1) && ct.Contains("ТОЛЬКО КОГДА ПЬЯНЫЙ")== (interrupt==0),
                    "Skaterz incomplete card number continuation depends on intervening "+interrupt);
            }

        var source=Load((Dictionary<string,object>)rows["026_skaterz_01"]);var randomSeen=new HashSet<int>();int attempts=0;
        while(randomSeen.Count<3 && attempts<64) {
            System.Threading.Thread.Sleep(20+(attempts*37)%41);
            attempts++;var story=SkaterzStart(source,"ultra-01-drive-random");var actions=new List<string>();story.OnActionTriggered+=(e)=>actions.Add(e.action);var all=Drain(story);
            var first=actions.FindAll(x=>x.Contains("destination"));if(first.Count!=1)throw new Exception("Skaterz random first destination count");
            var m=Regex.Match(first[0],@"[=:]\s*(\d+)");if(!m.Success)throw new Exception("Skaterz random destination syntax");int destination=int.Parse(m.Groups[1].Value);
            bool correct=story.FindVariable("yellow_found").value==1 && destination>=0&&destination<=2;int menus=0;
            while(story.GetChoices().Count>0) {if(++menus>20)throw new Exception("Skaterz original random full ride loop");story.ChooseAnswer(0);all.AddRange(Drain(story));}
            correct &= !story.can_continue && story.FindVariable("yellow_found").value==5 && story.FindVariable("_Car.Clean").value==2;
            correct &= actions.FindAll(x=>x.Contains("destination")).Count==3&&actions.Exists(x=>x.Contains("tip:")&&x.Contains("high"));
            if(!correct)throw new Exception("Skaterz actual original random ride/state");
            if(randomSeen.Add(destination))Check(correct,"Skaterz actual original RNG full ride first destination="+destination+" three visits/high tip/cleaning; actual run, no forced random edge");
        }
        Check(randomSeen.Count==3,"Skaterz all3actual original random alternatives sampled within64attempts; not all Cartesian prefixes");
        var unused=SkaterzStart(source,"ultra-01-hotel-emptyB");var unusedText=Drain(unused);
        Check(unusedText.Exists(t=>t.Contains("ВОТ ЭТО МЫ ВЛЯПАЛИСЬ"))&&unused.FindVariable("yellow_found").value==0,"Skaterz source-unreachable hotel emptyB translated; controlled direct entry, no repaired normal link/counter");
        var metadata=Strings(((Dictionary<string,object>)rows["026_skaterz_01"])["_user_datas"]);
        Check(metadata.Count==7&&metadata[0]=="passengercall: «Нам нужна… ваша… помощь!»"&&metadata.FindAll(x=>x.StartsWith("anim-link:")).Count==6,"Skaterz French formal help request/original prefix/six animation definitions; not new images");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2355.json");
        Check((string)card["fullname"]=="Ultra Skaterz"&&Convert.ToInt32(card["age"])==25&&Convert.ToInt32(card["gender"])==2,"Skaterz group card25/gender2 does not establish individual age or Red sex");
    }
    static PrompterStory SkaterzStart(Dialog d,string passage) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Car.Clean",3)},passage);return story;
    }
    static PrompterStory LEStart(Dialog d,string passage,int trust,int heard) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_LE.Trust.Driver",trust),new DialogVariable("_Driver.Heard.Deal",heard)},passage);return story;
    }
    static PrompterStory CarolinaStart(Dialog d,string passage,int killer,int deal) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Killer.Is.Judge",killer==0?1:0),
            new DialogVariable("_Killer.Is.Sandman",killer==1?1:0),new DialogVariable("_Killer.Is.Angel",killer==2?1:0),
            new DialogVariable("_Found.Clue.Today",0),new DialogVariable("_Driver.Deal.Carolina",deal)},passage);return story;
    }
    static PrompterStory StartWithJeromeFlag(Dialog d,string passage) {
        return StartWithJeromeFlag(d,passage,0);
    }
    static PrompterStory StartWithJeromeFlag(Dialog d,string passage,int burning) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Jerome.Burning.InCar",burning)},passage);return story;
    }
    static PrompterStory PeterFlags(Dialog d,string passage,int met,int killer) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Driver.Met.Alicia",(met&1)!=0?1:0),
            new DialogVariable("_Driver.Met.Julian",(met&2)!=0?1:0),new DialogVariable("_Driver.Met.Shohreh",(met&4)!=0?1:0),
            new DialogVariable("_Driver.Met.Peter",0),new DialogVariable("_Found.Clue.Today",0),
            new DialogVariable("_Killer.Is.Judge",killer==0?1:0),new DialogVariable("_Killer.Is.Sandman",killer==1?1:0),
            new DialogVariable("_Killer.Is.Angel",killer==2?1:0)},passage);return story;
    }
    static void ChiaraChecks(Dictionary<string,object> rows) {
        string ct;int courierRoutes=0;
        for(int chFirst=0;chFirst<2;chFirst++)for(int chSecond=0;chSecond<4;chSecond++)for(int chSpeed=0;chSpeed<2;chSpeed++)
        for(int chJob=0;chJob<2;chJob++)for(int chPay=0;chPay<2;chPay++)for(int chFood=0;chFood<2;chFood++) {
            if(chSecond!=3 && chSpeed==1)continue;
            var ch=ChiaraStart(Load((Dictionary<string,object>)rows["033_chiara_01"]));var emitted=new List<string>();
            ch.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(ch).ToArray());
            bool coherent=ch.FindVariable("_Driver.Met.Chiara").value==1 && HasText(ct,"тебе дорогу перерезала") && HasText(ct,"перестроился, не включив поворотник");
            ch.ChooseAnswer(chFirst);ct=string.Join("\n",Drain(ch).ToArray());
            coherent &= HasText(ct,"спорила с одним курьером")== (chFirst==0);
            coherent &= HasText(ct,"пластиковый пакет") && HasText(ct,"маленькими коробочками") && HasText(ct,"Обратно к ресторану");
            coherent &= ch.FindVariable("sleep").value==(chFirst==1?1:0);
            ch.ChooseAnswer(chSecond);ct=string.Join("\n",Drain(ch).ToArray());
            coherent &= HasText(ct,"СОВСЕМ не сезон")== (chSecond==0);
            coherent &= HasText(ct,"не для того, чтобы знакомиться с замечательными людьми")== (chSecond==1);
            coherent &= HasText(ct,"Вторая зима")== (chSecond==2);
            if(chSecond==3){coherent &= HasText(ct,"Ей нет и двадцати пяти") && HasText(ct,"сорок часов");ch.ChooseAnswer(chSpeed);ct=string.Join("\n",Drain(ch).ToArray());}
            int naps=(chFirst==1?1:0)+(chSecond==3?1:0);coherent &= ch.FindVariable("sleep").value==naps;
            coherent &= HasText(ct,"ты спас мне вечер")== (naps==2);
            coherent &= HasText(ct,"старые дорожные волки")== (chFirst==0 && chSecond==3);
            coherent &= emitted.Exists(a=>a.Contains("033_01"))==(chSecond==3 && chSpeed==0);
            ch.ChooseAnswer(chJob);ct=string.Join("\n",Drain(ch).ToArray());
            coherent &= HasText(ct,"Мой телефон. То есть алгоритм")== (chJob==0) && HasText(ct,"брать больше „смен“")== (chJob==0);
            coherent &= HasText(ct,"И вот ЭТО бесценно")== (chJob==0);
            coherent &= HasText(ct,"Маркетинг. Какой-то стартап")== (chJob==1);
            coherent &= HasText(ct,"который лапал меня за задницу")== (chJob==1);
            ch.ChooseAnswer(chPay);ct=string.Join("\n",Drain(ch).ToArray());
            coherent &= HasText(ct,"Компания платит гроши")== (chPay==0);
            coherent &= HasText(ct,"Руководителем команды")== (chPay==1);
            coherent &= HasText(ct,"Лицемерие желудка") && HasText(ct,"ларёк с такояки");
            ch.ChooseAnswer(chFood);ct=string.Join("\n",Drain(ch).ToArray());
            coherent &= HasText(ct,"Шарики из теста с осьминогом")== (chFood==1) && HasText(ct,"Рецепт моей бабушки")== (chFood==1);
            coherent &= emitted.Exists(a=>a.Contains("033_02"))==(chFood==1);
            coherent &= HasText(ct,"людям перед концертными залами") && HasText(ct,"кинотеатрами, театрами") && HasText(ct,"платит за поездку") && HasText(ct,"подходит к коллегам");
            var chTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= chTips.Count==(naps==2?2:1) && chTips[0].Contains("low");
            if(naps==2)coherent &= chTips[1].Contains("medium");
            coherent &= emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*start")).Count==2 && emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*stop")).Count==2;
            coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==0;
            Check(coherent,"Chiara complete delivery and return retain two naps, distinct jobs, informal speech and takoyaki plan "+chFirst+"/"+chSecond+"/"+chSpeed+"/"+chJob+"/"+chPay+"/"+chFood);courierRoutes++;
        }
        Check(courierRoutes==80,"Chiara all80original joint courier profiles retained; distinct naps/boss paths/pay questions/food knowledge and two original travel legs");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2362.json");var calls=Strings(((Dictionary<string,object>)rows["033_chiara_01"])["_user_datas"]);
        Check(Convert.ToInt32(card["age"])==26&&Convert.ToInt32(card["gender"])==0&&calls.Count==9&&calls[0]=="passengercall: «Нужна помощь с доставкой…»","Chiara female26card remains separate from narrator under25; useful delivery call plus8technical animations, not an invented address");
    }
    static void AdaChiaraLinkChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/ada_validation_routes.json");var row=(Dictionary<string,object>)rows["200_ada_01"];
        var before=(Dictionary<string,object>)reference["chiara_before"];List<string> handoffText,handoffActions;var previous=ReferenceRun((Dictionary<string,object>)rows["033_chiara_01"],before,(string)before["start"],MathieuGlobals(before["globals"]),out handoffText,out handoffActions);AngeliqueActions(before,handoffActions);int actualMet=MathieuValue(previous,"_Driver.Met.Chiara");Dictionary<string,object> after=null;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"])if(Convert.ToInt32(((Dictionary<string,object>)r["globals"])["_Driver.Met.Chiara"])==1&&Strings(r["passages"]).Contains("ada-01-destroy-but-chiara")){after=r;break;}
        if(after==null)throw new Exception("Ada actual courier reference missing");var g=new Dictionary<string,object>((Dictionary<string,object>)after["globals"]);g["_Driver.Met.Chiara"]=actualMet;var next=ReferenceRun(row,after,(string)reference["start"],MathieuGlobals(g),out handoffText,out handoffActions);AngeliqueActions(after,handoffActions);
        Check(actualMet==1&&!next.can_continue&&Strings(after["passages"]).Contains("ada-01-destroy-but-chiara")&&handoffText.Exists(x=>x.Contains("Вам кажется, вы её узнаёте. Одна из ваших клиенток.")),"Ada actual033Chiara ride flag selects probable femalecourier recollection; no explicit identity added");
    }
    static void AnitaChecks(Dictionary<string,object> rows) {
        string ct;int talkRoutes=0,quietRoutes=0;
        for(int anKnown=0;anKnown<2;anKnown++)for(int anIntro=0;anIntro<3;anIntro++)for(int anFollow=0;anFollow<2;anFollow++)for(int anExit=0;anExit<2;anExit++) {
            var an=GeraldineStart(Load((Dictionary<string,object>)rows["032_anita_01"]),"anita-geraldine-start",anKnown,(anIntro+anFollow+anExit)%4);
            var emitted=new List<string>();an.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(an).ToArray());
            bool coherent=ct.Contains("рестораном со звездой")== (anKnown==0) && an.FindVariable("_Pierrette.Restaurant.Known").value==1;
            coherent &= ct.Contains("другое такси") && ct.Contains("Подозреваемая садится");
            an.ChooseAnswer(anIntro);ct=string.Join("\n",Drain(an).ToArray());
            coherent &= ct.Contains("суперспособности")== (anIntro!=2) && ct.Contains("о новой идее") && ct.Contains("о бизнес-плане");
            an.ChooseAnswer(anFollow);ct=string.Join("\n",Drain(an).ToArray());
            coherent &= (ct.IndexOf("рассталась со своим мужем-идиотом",StringComparison.OrdinalIgnoreCase)>=0)== (anFollow==0);
            coherent &= ct.Contains("у родителей были деньги")== (anFollow==0);
            coherent &= ct.Contains("никудышным руководителем")== (anFollow==0);
            coherent &= ct.Contains("именно такой человек для меня") && ct.Contains("благополучие компании важнее всего остального");
            coherent &= Regex.Matches(ct,"сбавить обороты").Count==2 && ct.Contains("расплачивается за считаные мгновения") && ct.Contains("купюрами такими чистыми");
            coherent &= ct.Contains("отстёгивает ремень безопасности") && ct.Contains("Это она…") && ct.Contains("Показалось");
            coherent &= ct.Contains("Её взгляд становится пронзительным; она изучает вас.") && ct.Contains("Удачи вам до конца смены") && !ct.Contains("прощупывает вас") && !ct.Contains("пистолет");
            var anClues=emitted.FindAll(a=>a.Contains("clue:"));coherent &= anClues.Count==1 && anClues[0].Contains("clue_sand_customer_anita_01");
            coherent &= an.FindVariable("_Found.Clue.Today").value==0;
            coherent &= emitted.Exists(a=>a.Contains("032_04")) && !emitted.Exists(a=>a.Contains("032_02") || a.Contains("032_03"));
            coherent &= emitted.FindAll(a=>a.Contains("tip")).Count==1 && emitted.Exists(a=>a.Contains("tip") && a.Contains("high"));
            an.ChooseAnswer(anExit);ct=string.Join("\n",Drain(an).ToArray());
            coherent &= ct.Contains("Свет не зажигает")== (anExit==0) && ct.Contains("вас бьёт дрожь");
            Check(coherent,"Anita full conversation retains biographical owners, corporate justification and frightened identification "+anKnown+"/"+anIntro+"/"+anFollow+"/"+anExit);talkRoutes++;
        }
        for(int anKnown=0;anKnown<2;anKnown++)for(int anOrigin=0;anOrigin<4;anOrigin++)for(int topic=-1;topic<4;topic++)
        for(int outcome=0;outcome<6;outcome++)for(int anExit=0;anExit<2;anExit++) {
            if(topic>=0 && topic<=2 && outcome==5)continue;
            var an=GeraldineStart(Load((Dictionary<string,object>)rows["032_anita_01"]),"anita-geraldine-start",anKnown,(anOrigin+outcome+anExit)%4);
            var emitted=new List<string>();an.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(an);an.ChooseAnswer(anOrigin);Drain(an);
            if(anOrigin<3){an.ChooseAnswer(2);Drain(an);}
            an.ChooseAnswer(topic<0?1:0);ct=string.Join("\n",Drain(an).ToArray());
            bool coherent=true;if(topic>=0){an.ChooseAnswer(topic);ct=string.Join("\n",Drain(an).ToArray());}
            coherent &= an.FindVariable("restaurant").value==(topic==0?1:0) && an.FindVariable("friend").value==(topic==1?1:0);
            coherent &= an.FindVariable("saintgermain").value==(topic==2?1:0);
            coherent &= ct.Contains("шеф — замечательная женщина")== (topic==0);
            coherent &= ct.Contains("Великолепный ресторан")== (topic==0) && ct.Contains("резко поднимает голову")== (topic==3) && ct.Contains("руководит крупной компанией");
            coherent &= ct.Contains("Я вас не знаю")== (topic==3);
            bool looked=outcome==0;bool annoyed=false;
            if(outcome<2){an.ChooseAnswer(outcome==0?0:2);ct=string.Join("\n",Drain(an).ToArray());}
            else {
                an.ChooseAnswer(1);Drain(an);var labels=new List<string>();foreach(var answer in an.GetChoices())labels.Add(answer.GetText());
                coherent &= labels.Count==(topic>=0 && topic<=2?3:4);
                coherent &= labels.Exists(x=>x.Contains("ресторан"))==(topic!=0);
                coherent &= labels.Exists(x=>x.Contains("Подруга"))==(topic!=1);
                coherent &= labels.Exists(x=>x.Contains("район"))==(topic!=2);
                int answerIndex=outcome-2;annoyed=answerIndex<labels.Count-1;an.ChooseAnswer(answerIndex);ct=string.Join("\n",Drain(an).ToArray());
            }
            coherent &= ct.Contains("Как называется ваша компания")==looked;
            coherent &= ct.Contains("мне нужно сосредоточиться")==annoyed;
            coherent &= emitted.Exists(a=>a.Contains("032_03"))==looked && emitted.Exists(a=>a.Contains("032_02"))==!looked;
            coherent &= !emitted.Exists(a=>a.Contains("032_04")) && emitted.FindAll(a=>a.Contains("clue:")).Count==0;
            var anTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= anTips.Count==((looked || annoyed)?1:0);
            if(looked || annoyed)coherent &= anTips[0].Contains("low");
            an.ChooseAnswer(anExit);ct=string.Join("\n",Drain(an).ToArray());
            coherent &= ct.Contains("Свет не зажигает")== (anExit==0) && ct.Contains("вас бьёт дрожь");
            coherent &= emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*start")).Count==1 && emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*stop")).Count==1;
            Check(coherent,"Anita closed conversation retains topic exclusion, face recognition denial and distinct quiet/annoyed/look endings "+anKnown+"/"+anOrigin+"/"+topic+"/"+outcome+"/"+anExit);quietRoutes++;
        }
        Check(talkRoutes==24&&quietRoutes==432,"Anita all24 full-talking and432 closed joint profiles retained, three local topic flags/menu exclusions and distinct clue/tip/exit outcomes; not every Cartesian prefix");
    }
    static void GeraldineChecks(Dictionary<string,object> rows) {
        string ct;int gerRoutes=0,gerControls=0;
        for(int geKnown=0;geKnown<2;geKnown++)for(int geRestaurant=0;geRestaurant<3;geRestaurant++)for(int geMood=0;geMood<3;geMood++)
        for(int geBrand=0;geBrand<3;geBrand++)for(int geRevenge=0;geRevenge<4;geRevenge++)for(int geWatch=0;geWatch<2;geWatch++) {
            var ge=GeraldineStart(Load((Dictionary<string,object>)rows["031_geraldine_01"]),"anita-geraldine-start",geKnown,(geRestaurant+geMood+geBrand+geRevenge+geWatch)%4);
            var emitted=new List<string>();ge.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(ge).ToArray());
            bool coherent=ct.Contains("рестораном со звездой")== (geKnown==0);
            coherent &= ct.Contains("очень известным")== (geKnown==0) && !ct.Contains("о котором много слышали");
            coherent &= ct.Contains("Мне в Сен-Жермен") && ct.Contains("ей к Этуаль") && ct.Contains("ЖЕРАЛЬДИНА: «Точно");
            coherent &= ct.Contains("флисовой пижамы") && ge.FindVariable("_Pierrette.Restaurant.Known").value==1 && ge.GetChoices().Count==3;
            ge.ChooseAnswer(geRestaurant);Drain(ge);ge.ChooseAnswer(geMood);ct=string.Join("\n",Drain(ge).ToArray());
            coherent &= ct.Contains("Кондитерские Маре") && ct.Contains("четырнадцать по всей Франции") && ct.Contains("уволить половину сотрудников");
            coherent &= ct.Contains("доводилось мстить") && ct.Contains("все оставшиеся у меня деньги") && ct.Contains("Бутылка хорошего вина")== (geMood==0);
            ge.ChooseAnswer(geBrand);ct=string.Join("\n",Drain(ge).ToArray());coherent &= ct.Contains("Четыре месяца назад");
            int dismissal=(geRestaurant+geMood+geBrand+geRevenge)%3;ge.ChooseAnswer(dismissal);ct=string.Join("\n",Drain(ge).ToArray());
            coherent &= ct.Contains("продавать компанию крупной группе")== (dismissal!=0);
            coherent &= ct.Contains("продать оставшиеся акции") && ct.Contains("Если мой план сработает");
            coherent &= ct.Contains("СТЫД") && ct.Contains("Дала мне контакт своего специалиста");
            ge.ChooseAnswer(geRevenge);ct=string.Join("\n",Drain(ge).ToArray());
            coherent &= ct.Contains("Группа „Диамант“")== (geRevenge==0);
            coherent &= ct.Contains("ваши сотрудники останутся без работы")== (geRevenge==1);
            coherent &= ct.Contains("Цена мести")== (geRevenge==2);
            coherent &= ct.Contains("Что-то заметила дальше по улице")== (geRevenge!=3);
            int score=(geRestaurant==1?1:0)+(geMood==0?1:0)+(geBrand==0?1:0)+(geRevenge==2?1:0);
            coherent &= ge.FindVariable("liked").value==score;
            coherent &= ct.Contains("Мы понимаем друг друга")== (score>3);
            coherent &= ct.Contains("Продолжайте в том же духе")== (score>1 && score<=3);
            coherent &= ct.Contains("берегите себя")== (score<=1);
            coherent &= emitted.Exists(a=>a.Contains("031_02"))==(score>3);
            var geClues=emitted.FindAll(a=>a.Contains("clue:"));coherent &= geClues.Count==(geRevenge==0?2:1);
            coherent &= geClues[0].Contains("clue_sand_customer_geraldine_01");
            if(geRevenge==0)coherent &= geClues[1].Contains("clue_sand_customer_geraldine_02");
            coherent &= ge.FindVariable("_Found.Clue.Today").value==0;
            var geTips=emitted.FindAll(a=>a.Contains("tip"));coherent &= geTips.Count==(score>1?1:0);
            if(score>1)coherent &= geTips[0].Contains(score>3?"high":"medium");
            ge.ChooseAnswer(geWatch);ct=string.Join("\n",Drain(ge).ToArray());
            coherent &= ct.Contains("поднимает два пальца")== (geWatch==0) && emitted.Exists(a=>a.Contains("031_03"))==(geWatch==0);
            coherent &= ct.Contains("сила привычки")== (geWatch==0) && !ct.Contains("завсегдатай");
            coherent &= emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*start")).Count==1 && emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*stop")).Count==1;
            Check(coherent,"Geraldine full route retains known restaurant, four affinity increments, moral choices and strict endings "+geKnown+"/"+geRestaurant+"/"+geMood+"/"+geBrand+"/"+geRevenge+"/"+geWatch);gerRoutes++;
        }
        foreach(int geAnswer in new int[]{0,1}) {
            var ge=GeraldineStart(Load((Dictionary<string,object>)rows["031_geraldine_01"]),"geraldine-01-restaurant-var-no",0,3);
            Drain(ge);bool coherent=ge.GetChoices().Count==2;ge.ChooseAnswer(geAnswer);ct=string.Join("\n",Drain(ge).ToArray());
            Check(coherent && ct.Contains("Совершенно невероятные морепродукты") && ge.FindVariable("_Pierrette.Restaurant.Known").value==1 && ge.FindVariable("liked").value==0,
                "Geraldine separately entered unknown-restaurant menu retains both answers "+geAnswer);gerControls++;
        }
        Check(gerRoutes==432&&gerControls==2,"Geraldine all432 original full joint profiles plus2explicit source-menu controls retained; not all Cartesian dismissal prefixes");
    }
    static PrompterStory GeraldineStart(Dialog d,string passage,int known,int killer) {
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(a)=>{};
        story.StartDialog(new List<DialogVariable>{new DialogVariable("_Pierrette.Restaurant.Known",known),new DialogVariable("_Found.Clue.Today",0),
            new DialogVariable("_Killer.Is.Judge",killer==0?1:0),new DialogVariable("_Killer.Is.Sandman",killer==1?1:0),
            new DialogVariable("_Killer.Is.Angel",killer==2?1:0)},passage);return story;
    }
    static void ChooseLabel(PrompterStory story,string contains) {
        var choices=story.GetChoices();
        for(int i=0;i<choices.Count;i++)if(choices[i].GetText().IndexOf(contains,StringComparison.OrdinalIgnoreCase)>=0){story.ChooseAnswer(i);return;}
        throw new Exception("Choice label missing: "+contains);
    }
    static PrompterStory FragonardFinal(Dictionary<string,object> rows,string passage,int met,int joker) {
        var d=Load((Dictionary<string,object>)rows["014_fragonard_08"]);
        var flags=new List<DialogVariable>{new DialogVariable("_Driver.Met.Fragonard",met),
            new DialogVariable("_Judge.Joker",joker),new DialogVariable("_GOT_KILLED",0),
            new DialogVariable("_Fragonard.Is.Dead",0),new DialogVariable("_Judge.Is.Arrested",0)};
        var story=new PrompterStory(d,false);story.OnActionTriggered+=(evt)=>{};story.StartDialog(flags,passage);return story;
    }
    static string FragonardToBench(PrompterStory story) {
        // Starting here: story of his parents. Let him continue, agree to detour,
        // then let him go to the bench. Original commands are drained as usual.
        story.ChooseAnswer(2);var text=string.Join("\n",Drain(story).ToArray());
        story.ChooseAnswer(1);text+="\n"+string.Join("\n",Drain(story).ToArray());
        story.ChooseAnswer(1);text+="\n"+string.Join("\n",Drain(story).ToArray());return text;
    }
    static void Fragonard08Checks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/fragonard08_validation_routes.json");var row=(Dictionary<string,object>)rows["014_fragonard_08"];
        var seen=new HashSet<string>();var edges=new HashSet<string>();var endings=new HashSet<string>();int number=0;
        foreach(Dictionary<string,object> raw in (IEnumerable)reference["routes"]) {
            var r=FactoredTextRoute(raw,reference);List<string> text,actions;var story=ReferenceRun(row,r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,seen,edges);
            var p=new HashSet<string>(Strings(r["passages"]));var globals=(Dictionary<string,object>)r["globals"];bool dead=p.Contains("frag-08-gameover"),suicide=p.Contains("frag-08-end"),arrest=p.Contains("frag-08-garage-end");string all=string.Join("\n",text.ToArray());
            bool ok=!story.can_continue&&story.GetChoices().Count==0&&(dead?1:0)+(suicide?1:0)+(arrest?1:0)==1;
            ok &= MathieuValue(story,"_GOT_KILLED")== (dead?1:0)&&MathieuValue(story,"_Fragonard.Is.Dead")== (suicide?1:0)&&MathieuValue(story,"_Judge.Is.Arrested")== (arrest?1:0);
            ok &= MathieuValue(story,"asset")== (p.Contains("frag-08-asset-mentor")?1:0)&&p.Contains("frag-08-asset-intro")== (p.Contains("frag-08-reveal")&&Convert.ToInt32(globals["_Judge.Joker"])==1);
            ok &= actions.FindAll(a=>a=="tip: none").Count==1&&actions.FindAll(a=>a=="travel: start").Count==(arrest?1:0)&&actions.FindAll(a=>a=="travel: stop").Count==(arrest?2:1);
            ok &= !actions.Exists(a=>a.StartsWith("money:")||a.StartsWith("fare:")||a.StartsWith("clue:"));
            if(p.Contains("frag-08-story-mesrineknown-introA"))ok &= all.Contains("Враг общества номер один");
            if(p.Contains("frag-08-showdown-lie2"))ok &= text.Contains("Он кивает, огорчённый вашим ответом.");
            if(p.Contains("frag-08-asset"))ok &= text.Contains("Он поднимает голову. Глаза слегка дрожат. Он колеблется. Внутри у него паника.");
            if(p.Contains("frag-08-reveal"))ok &= all.Contains("Знаешь")&&all.Contains("перешёл с вами на «ты»");
            if(p.Contains("frag-08-story4"))ok &= text.Contains("ФРАГОНАР: «А потом вы поедете к месту встречи, и Бюссе меня заберёт. Что скажете?»");
            if(p.Contains("frag-08-story2"))ok &= text.Contains("В голосе появляется доверительная нотка…");
            if(p.Contains("frag-08-montmartre-dontdoit-didntsayanything"))ok &= text.Contains("Он некоторое время пристально смотрит на вас, кивает и чуть взволнованным голосом произносит…");
            if(p.Contains("frag-08-reveal-known"))ok &= Convert.ToInt32(globals["_Driver.Met.Fragonard"])==1&&!all.Contains("Рак. По всему телу");
            if(p.Contains("frag-08-reveal-unknown"))ok &= Convert.ToInt32(globals["_Driver.Met.Fragonard"])==0&&all.Contains("Рак. По всему телу")&&all.Contains("очередное… лекарство");
            if(dead)ok &= all.Contains("Вы заслуживали лучшего")&&all.Contains("Семь лет отсидели вместо неё");
            if(suicide)ok &= text.Contains("За секунду он достаёт пистолет из кармана и прижимает под подбородком.");
            if(arrest)ok &= text.IndexOf("Они выкрикивают имя пассажира. Он кладёт пистолет рядом и поднимает руки.")<text.IndexOf("ФРАГОНАР: «Выхожу! Руки подняты! Я безоружен!»")&&text.Contains("Дверь с вашей стороны открывается. Резким рывком вас вытаскивают на землю.");
            endings.Add(dead?"driver death":suicide?"suicide":"arrest");
            Check(ok,"Fragonard08 full representative history #"+(++number)+" exact FR lines/menus/state/actions, source vous exceptions and distinct ending");
        }
        Check(number==166&&seen.Count==65&&edges.Count==56&&endings.Count==3,"Fragonard08 166 representative full routes/4profiles cover65blocks198lines56answers/all3endings;not every Cartesian prefix");
        Check(Strings(row["_user_datas"]).Count==14&&string.Join("\n",Strings(row["_user_datas"]).ToArray())==string.Join("\n",Strings(reference["user_datas"]).ToArray()),"Fragonard08 all14 original animation definitions; no invented passenger call");
    }
    static void GildaChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/gilda_validation_routes.json");
        var scenes=new Dictionary<string,Dictionary<string,object>>();int journeys=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];scenes[name]=scene;var covered=new HashSet<string>();var answers=new HashSet<string>();int count=0;
            foreach(Dictionary<string,object> route in (IEnumerable)scene["routes"]) {
                List<string> text,actions;var story=ReferenceRun((Dictionary<string,object>)rows[name],route,(string)scene["start"],MathieuGlobals(route["globals"]),out text,out actions);
                AngeliqueActions(route,actions);ReferenceCoverage(route,covered,answers);var path=new HashSet<string>(Strings(route["passages"]));
                bool ok=!story.can_continue&&story.GetChoices().Count==0&&actions.FindAll(a=>a=="travel: start").Count==1&&actions.FindAll(a=>a=="travel: stop").Count==1;
                if(name.EndsWith("01")) {
                    ok &= MathieuValue(story,"_Driver.Met.Gilda")==1&&actions.FindAll(a=>a.Contains("tip:")).Count==1&&actions.Exists(a=>a=="tip: low");
                    ok &= MathieuValue(story,"_Gilda.Job.Know")== (path.Contains("gilda-01-abouthim-great-job")?1:0);
                    ok &= MathieuValue(story,"_Found.Clue.Today")== (path.Contains("gilda-01-jesus")?1:0);
                    ok &= MathieuValue(story,"city")== (path.Contains("gilda-01-silence3")?1:0);
                } else {
                    bool cops=path.Contains("gilda-02-cops");ok &= MathieuValue(story,"_GOT_KILLED")== (cops?0:1)&&MathieuValue(story,"_Angel.Is.Caught")== (cops?1:0);
                    ok &= !actions.Exists(a=>a.Contains("tip:")||a.Contains("clue:"));
                    ok &= MathieuValue(story,"_Angel.Joker.Used")== (path.Contains("gilda-02-joker")?1:0);
                    if(cops) {string all=string.Join("\n",text.ToArray());ok &= all.IndexOf("нож падает")<all.IndexOf("Она бежит")&&all.Contains("она мертва");}
                }
                Check(ok,"Gilda complete ordinary journey "+name+" #"+count+" exact text/menu/state/actions and ending");count++;journeys++;
            }
            Check(covered.Count==Convert.ToInt32(scene["passages_covered"])&&answers.Count==Convert.ToInt32(scene["answers_covered"]),"Gilda ordinary coverage "+name+" passages="+covered.Count+" answers="+answers.Count);
        }
        int controls=0;foreach(Dictionary<string,object> route in (IEnumerable)reference["controlled_fragments"]) {
            // Declared local variables shadow globals. Explicit direct-start tests
            // therefore initialize a private dialog copy, not the exported input.
            var controlledRow=new Dictionary<string,object>((Dictionary<string,object>)rows[(string)route["scene"]]);var initial=(Dictionary<string,object>)route["globals"];var variables=new List<object>();
            foreach(Dictionary<string,object> v in (IEnumerable)controlledRow["_variables"]) {var copy=new Dictionary<string,object>(v);if(initial.ContainsKey((string)v["_name"]))copy["_value"]=initial[(string)v["_name"]];variables.Add(copy);}controlledRow["_variables"]=variables;
            List<string> text,actions;var story=ReferenceRun(controlledRow,route,(string)route["start"],MathieuGlobals(route["globals"]),out text,out actions);AngeliqueActions(route,actions);
            Check(!story.can_continue&&story.GetChoices().Count==0,"Gilda controlled unreachable fragment "+(string)route["start"]+" exact source reference, not ordinary trip");controls++;
        }
        int handoffs=0;foreach(int job in new int[]{0,1}) {
            Dictionary<string,object> before=null,after=null;
            foreach(Dictionary<string,object> r in (IEnumerable)scenes["096_gilda_01"]["routes"])if(Convert.ToInt32(((Dictionary<string,object>)r["expected_variables"]).ContainsKey("_Gilda.Job.Know")?((Dictionary<string,object>)r["expected_variables"])["_Gilda.Job.Know"]:0)==job){before=r;break;}
            foreach(Dictionary<string,object> r in (IEnumerable)scenes["096_gilda_02"]["routes"]) {var g=(Dictionary<string,object>)r["globals"];if(Convert.ToInt32(g["_Gilda.Met"])==0&&Convert.ToInt32(g["_Gilda.Job.Know"])==job&&Convert.ToInt32(g["_Angel.Joker"])==0&&Strings(r["passages"]).Contains("gilda-02-intro-goodevening-opera-bad")){after=r;break;}}
            if(before==null||after==null)throw new Exception("Gilda actual handoff reference missing");
            List<string> text,actions;var first=ReferenceRun((Dictionary<string,object>)rows["096_gilda_01"],before,(string)scenes["096_gilda_01"]["start"],MathieuGlobals(before["globals"]),out text,out actions);
            var inherited=new Dictionary<string,object>((Dictionary<string,object>)after["globals"]);inherited["_Driver.Met.Gilda"]=MathieuValue(first,"_Driver.Met.Gilda");inherited["_Gilda.Job.Know"]=MathieuValue(first,"_Gilda.Job.Know");
            var second=ReferenceRun((Dictionary<string,object>)rows["096_gilda_02"],after,(string)scenes["096_gilda_02"]["start"],MathieuGlobals(inherited),out text,out actions);AngeliqueActions(after,actions);
            Check(MathieuValue(second,"_Driver.Met.Gilda")==1&&MathieuValue(second,"_Gilda.Met")==0&&text.Contains("ГИЛЬДА: «Добрый вечер, месье»."),"Gilda actual01to02 raw handoff job="+job+" preserves source Met alias gap; external alias1 separately tested");handoffs++;
        }
        var ui=ReferenceFile("../translation/ui_ru.json");var mapping=ReferenceFile("revision/data/Russian_UI/full_translation_mapping.json");var checkedUI=ReferenceFile("localization/contexts/096_gilda_ui.json");int keys=0;
        foreach(Dictionary<string,object> k in (IEnumerable)checkedUI["keys"]){if((string)ui[(string)k["key"]]!=(string)k["russian"]||(string)mapping[(string)k["english"]]!=(string)k["russian"])throw new Exception("Gilda UI mismatch");keys++;}
        Check(keys==3&&((string)ui["096_gilda.bio"]).Contains("декоратор")&&((string)ui["096_gilda.bio"]).Contains("смерти матери"),"Gilda3reviewed UI keys profession/mother/father/faith preserve original source scope");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2401.json");int encounters=0;bool cardOK=(string)card["fullname"]=="Gilda Berger"&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["age"])==28&&Convert.ToInt32(card["related_to_killers"])==4;
        foreach(Dictionary<string,object> e in (IEnumerable)card["encounters"]) {cardOK &= Convert.ToInt32(e["appears_only_for_killer"])==4&&Convert.ToInt32(e["has_clues_for_killer"])==(encounters==0?4:0);encounters++;}
        Check(cardOK&&encounters==2,"Gilda female28 sourcecard two Angel4 encounters ordinaryclue/climax; no Unity appearance selection claim");
        Check(journeys==5281&&controls==3&&handoffs==2,"Gilda summary5281ordinary full journeys/3controlled fragments/2actual rawhandoffs/129passages79answers");
    }
    static void AlexandreChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/alexandre_validation_routes.json");var scenes=new Dictionary<string,Dictionary<string,object>>();int journeys=0;int met=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];scenes[name]=scene;var covered=new HashSet<string>();var answers=new HashSet<string>();int number=0;
            foreach(Dictionary<string,object> route in (IEnumerable)scene["routes"]) {
                List<string> text,actions;var story=ReferenceRun((Dictionary<string,object>)rows[name],route,(string)scene["start"],MathieuGlobals(route["globals"]),out text,out actions);AngeliqueActions(route,actions);ReferenceCoverage(route,covered,answers);
                var path=new HashSet<string>(Strings(route["passages"]));bool ok=!story.can_continue&&story.GetChoices().Count==0;
                if(name.EndsWith("01")) {
                    met=MathieuValue(story,"_Driver.Met.Alexandre");var tips=actions.FindAll(a=>a.Contains("tip:"));
                    ok &= met==1&&MathieuValue(story,"_Alexandre.Like")<=3&&MathieuValue(story,"_Sandman.Asset")== (path.Contains("alex-01-story-anger")?1:0);
                    ok &= tips.Count==(path.Contains("alex-01-story-heat")?2:1)&&tips[0]=="tip: none"&&(!path.Contains("alex-01-story-heat")||tips[1]=="tip: high");
                    ok &= actions.FindAll(a=>a=="travel: start").Count==1&&actions.FindAll(a=>a=="travel: stop").Count==1&&actions.FindAll(a=>a=="clue: poi_sand_cemetery").Count==1;
                    string all=string.Join("\n",text.ToArray());ok &= all.Contains("Даниэль")&&!all.Contains("летают ли киты")&&all.Contains("А вдруг этот старик");
                } else {
                    bool arrest=path.Contains("alexandre-02-arrest");int ending=MathieuValue(story,"_Sandman.Ending");
                    ok &= MathieuValue(story,"_GOT_KILLED")== (arrest?0:1)&&MathieuValue(story,"_Sandman.Is.Arrested")== (arrest?1:0)&&!actions.Exists(a=>a.Contains("tip:")||a.Contains("fare:")||a.Contains("clue:"));
                    ok &= !path.Contains("alexandre-02-boetie-story01-cop-joker")||Convert.ToInt32(((Dictionary<string,object>)route["globals"])["_Sandman.Asset"])==1;
                    if(path.Contains("alexandre-02-cop-arrival"))ok &= ending==(MathieuValue(story,"_Alexandre.Like")>5?10:20);
                    if(path.Contains("alexandre-02-reveal03-gun-story"))ok &= ending==30;
                    if(path.Contains("alexandre-02-reveal03-cop-truth"))ok &= MathieuValue(story,"_Alexandre.Lie")==0;
                    if(path.Contains("alexandre-02-reveal03-cop-lie"))ok &= MathieuValue(story,"_Alexandre.Lie")==1;
                    if(path.Contains("alexandre-02-death"))ok &= text.FindAll(t=>t.Contains("вонзает иглу")).Count==1&&text.Exists(t=>t.Contains("в шею сзади"))&&text.Exists(t=>t.Contains("к вашему виску"))&&!text.Exists(t=>t.Contains("к вашему лбу"))&&text.Contains("…");
                    if(path.Contains("alexandre-02-boetie-story01-cops"))ok &= text.Contains("Он кивает, почти разочарованно.");
                    if(path.Contains("alexandre-02-killher-no"))ok &= text.Contains("Он кивает.");
                    if(path.Contains("alexandre-02-boetie-story01-cop-confess-hubA"))ok &= text.Exists(t=>t.Contains("Он жаловался, что дочь осуждают, оскорбляют, винят."));
                    if(path.Contains("alexandre-02-boetie-story01-cop-confess"))ok &= text.Contains("«В машине микрофон…»");
                    if(path.Contains("alexandre-02-reveal02"))ok &= text.Exists(t=>t.Contains("Журналистка"));
                    if(path.Contains("alexandre-02-cop-arrival"))ok &= text.Exists(t=>t.Contains("Микаэлы Блан и Анри дю Тийё"));
                }
                Check(ok,"Alexandre complete ordinary journey "+name+" #"+(++number)+" original text/menu/state/actions and case-scoped endings");journeys++;
            }
            Check(covered.Count==Convert.ToInt32(scene["passages_covered"])&&answers.Count==Convert.ToInt32(scene["answers_covered"]),"Alexandre full ordinary coverage "+name+" passages="+covered.Count+" answers="+answers.Count);
        }
        int thresholds=0;foreach(Dictionary<string,object> r in (IEnumerable)reference["controlled_thresholds"]) {
            List<string> text,actions;var story=ReferenceRun((Dictionary<string,object>)rows["097_alexandre_02"],r,(string)r["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);int like=Convert.ToInt32(((Dictionary<string,object>)r["globals"])["_Alexandre.Like"]);
            Check(MathieuValue(story,"_Sandman.Ending")== (like>5?10:20)&&MathieuValue(story,"_GOT_KILLED")==0,"Alexandre controlled strict Like threshold "+like+" original ending="+MathieuValue(story,"_Sandman.Ending"));thresholds++;
        }
        int inheritedCount=0;var flags=Strings(reference["joint_flags"]);
        foreach(Dictionary<string,object> state in (IEnumerable)reference["actual_first_states"]) {
            Dictionary<string,object> before=null,after=null;
            foreach(Dictionary<string,object> r in (IEnumerable)scenes["097_alexandre_01"]["routes"]) {bool same=true;var v=(Dictionary<string,object>)r["expected_variables"];foreach(string k in flags)same &= Convert.ToInt32(v.ContainsKey(k)?v[k]:0)==Convert.ToInt32(state[k]);if(same){before=r;break;}}
            foreach(Dictionary<string,object> r in (IEnumerable)scenes["097_alexandre_02"]["routes"]) {bool same=true;var g=(Dictionary<string,object>)r["globals"];foreach(string k in flags)same &= Convert.ToInt32(g[k])==Convert.ToInt32(state[k]);if(same){after=r;break;}}
            if(before==null||after==null)throw new Exception("Alexandre inherited joint state missing");
            List<string> t,a;var previous=ReferenceRun((Dictionary<string,object>)rows["097_alexandre_01"],before,(string)scenes["097_alexandre_01"]["start"],MathieuGlobals(before["globals"]),out t,out a);
            var actual=new Dictionary<string,object>((Dictionary<string,object>)after["globals"]);foreach(string k in flags)actual[k]=MathieuValue(previous,k);
            var next=ReferenceRun((Dictionary<string,object>)rows["097_alexandre_02"],after,(string)scenes["097_alexandre_02"]["start"],MathieuGlobals(actual),out t,out a);AngeliqueActions(after,a);
            Check(MathieuValue(previous,"_Driver.Met.Alexandre")==1&&t.Exists(x=>x.Contains("вас не узнал")),"Alexandre actual01to02 inherited joint Like/Asset/Met state "+(++inheritedCount)+" no impossible ordinary combination");
        }
        var cop=(Dictionary<string,object>)reference["cop_followup"];var copPassages=new HashSet<string>();var copAnswers=new HashSet<string>();int copRoutes=0;
        foreach(Dictionary<string,object> r in (IEnumerable)cop["routes"]) {
            List<string> t,a;var story=ReferenceRun((Dictionary<string,object>)rows["100_cop_04"],r,(string)cop["start"],MathieuGlobals(r["globals"]),out t,out a);AngeliqueActions(r,a);ReferenceCoverage(r,copPassages,copAnswers);var g=(Dictionary<string,object>)r["globals"];string all=string.Join("\n",t.ToArray());int ending=Convert.ToInt32(g["_Sandman.Ending"]);
            bool ok=!story.can_continue&&story.GetChoices().Count==0;
            if(ending==10)ok &= all.Contains("увезли")&&all.Contains("всё рассказывает");
            if(ending==20)ok &= all.Contains("удастся ли его спасти")&&all.Contains("попытался покончить")&&all.Contains("Александр выживет");
            if(ending==30)ok &= all.Contains("арестовали")&&all.Contains("Никто не пострадал")&&all.Contains("солгали Александру");
            ok &= all.Contains("никогда не сидел")== (Convert.ToInt32(g["_Alexandre.Lie"])==1);
            Check(ok,"Alexandre cop04 complete followup source ending/lie profile #"+(++copRoutes)+" uncertain20/live10/uninjured30 and prison truth");
        }
        Check(copPassages.Count==22&&copAnswers.Count==6,"Alexandre cop04 source coverage22passages/6answers under six external end/lie profiles");
        var actualOutcomes=new HashSet<string>();int handoffs=0;
        foreach(Dictionary<string,object> r in (IEnumerable)scenes["097_alexandre_02"]["routes"]) {
            var path=Strings(r["passages"]);if(!path.Contains("alexandre-02-arrest"))continue;var expected=(Dictionary<string,object>)r["expected_variables"];int end=Convert.ToInt32(expected["_Sandman.Ending"]),lie=Convert.ToInt32(expected["_Alexandre.Lie"]);string key=end+"/"+lie;if(!actualOutcomes.Add(key))continue;
            Dictionary<string,object> after=null;foreach(Dictionary<string,object> candidate in (IEnumerable)cop["routes"]) {var cg=(Dictionary<string,object>)candidate["globals"];if(Convert.ToInt32(cg["_Sandman.Ending"])==end&&Convert.ToInt32(cg["_Alexandre.Lie"])==lie){after=candidate;break;}}
            if(after==null)throw new Exception("Alexandre actual cop handoff missing");List<string> t,a;
            var done=ReferenceRun((Dictionary<string,object>)rows["097_alexandre_02"],r,(string)scenes["097_alexandre_02"]["start"],MathieuGlobals(r["globals"]),out t,out a);var g=new Dictionary<string,object>((Dictionary<string,object>)after["globals"]);foreach(string k in new string[]{"_Sandman.Ending","_Alexandre.Lie","_Sandman.Is.Arrested","_GOT_KILLED"})g[k]=MathieuValue(done,k);
            var next=ReferenceRun((Dictionary<string,object>)rows["100_cop_04"],after,(string)cop["start"],MathieuGlobals(g),out t,out a);AngeliqueActions(after,a);
            Check(MathieuValue(done,"_Sandman.Is.Arrested")==1&&MathieuValue(done,"_GOT_KILLED")==0&&!next.can_continue,"Alexandre actual02to100cop04 inherited outcome "+key+" ending and prison lie preserved");handoffs++;
        }
        var sleep=SantaSleepCompile((string)reference["sleep_script"]);var ownFragment=(Dictionary<string,object>)reference["sleep_fragment"];var higher=(Dictionary<string,object>)ReferenceFile("localization/houssine_validation_routes.json")["sleep_fragments"];
        for(int mode=0;mode<3;mode++) {
            var f=mode==0?ownFragment:(Dictionary<string,object>)higher[mode==1?"sleep-client-090houssine":"sleep-client-077santa"];var wanted=Strings(f["texts"]);var story=new PrompterStory(sleep,false);var emitted=new List<string>();story.OnActionTriggered+=(e)=>emitted.Add(e.action);
            story.StartDialog(new List<DialogVariable>{new DialogVariable("_Day.Number",5),new DialogVariable("_Driver.Met.Alexandre",met),new DialogVariable("_Driver.Met.Houssine",mode>0?1:0),new DialogVariable("_Driver.Met.Santa",mode==2?1:0)},(string)reference["sleep_start"]);
            var text=new List<string>();int guard=0;while(story.can_continue&&text.Count<wanted.Count){if(++guard>40)throw new Exception("Alexandre sleep fragment loop");string line=story.GetNextLine();if(line!=null)text.Add(line);}
            bool same=met==1&&text.Count==wanted.Count;for(int i=0;i<text.Count&&same;i++)same &= text[i]==wanted[i];
            Check(same&&(mode>0||emitted.Exists(a=>a.Contains("097_00"))),"Alexandre actual01flag in compiled930day5 memory / source priority Santa>Houssine>Alexandre mode="+mode+" fragment only");
        }
        var absent=new PrompterStory(sleep,false);var absentActions=new List<string>();absent.OnActionTriggered+=(e)=>absentActions.Add(e.action);absent.StartDialog(new List<DialogVariable>{new DialogVariable("_Day.Number",5),new DialogVariable("_Driver.Met.Alexandre",0),new DialogVariable("_Driver.Met.Houssine",0),new DialogVariable("_Driver.Met.Santa",0)},(string)reference["sleep_start"]);Drain(absent);
        Check(!absentActions.Exists(a=>a.Contains("097_00")),"Alexandre missing encounter skips selected compiled930memory; fallback tail only");
        var ui=ReferenceFile("../translation/ui_ru.json");var mapping=ReferenceFile("revision/data/Russian_UI/full_translation_mapping.json");var reviewedUI=ReferenceFile("localization/contexts/097_alexandre_ui.json");int keys=0;
        foreach(Dictionary<string,object> k in (IEnumerable)reviewedUI["keys"]){if((string)ui[(string)k["key"]]!=(string)k["russian"]||(string)mapping[(string)k["english"]]!=(string)k["russian"])throw new Exception("Alexandre11UIkeys mismatch");keys++;}
        Check(keys==11&&((string)ui["097_alexandre.bio"]).Contains("Почтальон")&&((string)ui["clue_sand_cemetery_02.title"]).Contains("права на врачебную практику"),"Alexandre11reviewed UI keys current profession/former medicine/fire/land and source scopes");
        var shared=(Dictionary<string,object>)reviewedUI["shared_fallback"];bool sharedOK=(string)mapping[(string)shared["english"]]==(string)shared["russian"];int sharedKeys=0;foreach(string k in Strings(shared["keys"])){sharedOK &= (string)ui[k]==(string)shared["russian"];sharedKeys++;}
        Check(sharedOK&&sharedKeys==4,"Alexandre shared SuspectsIntel fallback matches all4existing keyed meanings");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2402.json");bool cardOK=(string)card["fullname"]=="Alexandre Leclerc"&&Convert.ToInt32(card["gender"])==3&&Convert.ToInt32(card["age"])==62&&Convert.ToInt32(card["related_to_killers"])==2;int encounters=0;
        foreach(Dictionary<string,object> e in (IEnumerable)card["encounters"]){cardOK &= Convert.ToInt32(e["appears_only_for_killer"])==2&&Convert.ToInt32(e["has_clues_for_killer"])==(encounters==0?2:0)&&Convert.ToInt32(e["is_climax"])==(encounters==0?0:1);encounters++;}
        Check(cardOK&&encounters==2,"Alexandre male62sourcecard two Sandman2 encounters ordinaryclue/climax; no Unity selector claim");
        foreach(string name in new string[]{"097_alexandre_01","097_alexandre_02"}) {
            var metadata=Strings(((Dictionary<string,object>)rows[name])["_user_datas"]);
            Check(metadata.Count==13&&metadata.FindAll(x=>x.StartsWith("anim-link:")).Count==12&&metadata[0].StartsWith("passengercall: ")&&metadata[0].Contains(name.EndsWith("01")?"«Мне в… э… Иври».":"«Мне понадобятся ваши услуги…»"),"Alexandre source invitation and12animation definitions "+name+" no new icon images");
        }
        Check(journeys==1483&&thresholds==2&&inheritedCount==7&&copRoutes==30&&handoffs==5,"Alexandre summary1483ordinary full routes/101passages79answers/7actual01states/5actualcopoutcomes/2thresholds/compiledSleep fragments");
    }
    static void AdaChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/ada_validation_routes.json");var row=(Dictionary<string,object>)rows["200_ada_01"];var covered=new HashSet<string>();var answers=new HashSet<string>();var orders=new HashSet<string>();var repeated=new HashSet<string>();int histories=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)reference["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,covered,answers);var path=new HashSet<string>(Strings(r["passages"]));var topics=Strings(r["topic_order"]);string order=string.Join("/",topics.ToArray());orders.Add(order);if(new HashSet<string>(topics).Count<3)repeated.Add(order);
            bool ok=!story.can_continue&&story.GetChoices().Count==0&&MathieuValue(story,"question")==3&&topics.Count==3&&MathieuValue(story,"question-origin")==1;
            ok &= actions.FindAll(a=>a=="tip: high").Count==1&&actions.FindAll(a=>a.Contains("tip:")).Count==1&&actions.FindAll(a=>a=="travel: start").Count==1&&actions.FindAll(a=>a=="travel: stop").Count==1&&!actions.Exists(a=>a.Contains("clue:")||a.Contains("fare:"))&&MathieuValue(story,"_Driver.Met.Ada")==0;
            int met=Convert.ToInt32(((Dictionary<string,object>)r["globals"])["_Driver.Met.Chiara"]);ok &= path.Contains("ada-01-destroy-but-chiara")== (met==1&&path.Contains("ada-01-destroy-but"));
            if(path.Contains("ada-01-destroy-but"))ok &= text.Exists(t=>t.Contains("Она звенит звонком"))&&text.Exists(t=>t.Contains("добывшего металл для велосипеда"))&&text.Exists(t=>t.Contains("пиццу"));
            ok &= text.Exists(t=>t.Contains("Ады Лавлейс"))&&text.Exists(t=>t.Contains("Вирджиния Вулф"))&&text.Exists(t=>t.Contains("Маргерит Юрсенар"))&&text.Exists(t=>t.Contains("Маттиас R. и Меган H."));
            ok &= text.Exists(t=>t.Contains("свысока"))==path.Contains("ada-01-destroy-yes");
            ok &= path.Contains("ada-01-end-matrix")==path.Contains("ada-01-destroy-matrix")&&path.Contains("ada-01-end-terminator")==path.Contains("ada-01-destroy-terminator");
            if(path.Contains("ada-01-destroy-matrix"))ok &= text.Exists(t=>t.Contains("захватывающими дух, если бы могла дышать"))&&text.Exists(t=>t.Contains("батарейки"));
            if(path.Contains("ada-01-destroy-terminator"))ok &= text.Exists(t=>t.Contains("драки в туалете"))&&text.Exists(t=>t.Contains("ядерную бомбу"));
            ok &= text.Exists(t=>t.Contains("начинайте инвестировать"))&&!text.Exists(t=>t.Contains("инвестировать в себя"));
            Check(ok,"Ada exact complete finite history #"+(++histories)+" original text/menu/state/actions, actual topic order and speaker context");
        }
        Check(histories==3456&&covered.Count==48&&answers.Count==25&&orders.Count==6&&repeated.Count==3,"Ada all3456finite histories cover48passages/25answers/twoMetprofiles/sixorders-three repeats; original loops preserved");
        int controls=0;foreach(Dictionary<string,object> r in (IEnumerable)reference["controlled_movie_finals"]) {
            var copy=new Dictionary<string,object>(row);var initial=(Dictionary<string,object>)r["globals"];var variables=new List<object>();foreach(Dictionary<string,object> v in (IEnumerable)copy["_variables"]) {var item=new Dictionary<string,object>(v);if(initial.ContainsKey((string)v["_name"]))item["_value"]=initial[(string)v["_name"]];variables.Add(item);}copy["_variables"]=variables;
            List<string> text,actions;var story=ReferenceRun(copy,r,(string)r["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);
            Check(!story.can_continue&&story.GetChoices().Count==0&&text.Exists(t=>t.Contains("батарейки"))==(Convert.ToInt32(initial["movie-matrix"])==1),"Ada controlled final localmovie flags priority #"+(++controls)+" private copy, source unchanged");
        }
        AdaChiaraLinkChecks(rows);
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2405.json");bool cardOK=(string)card["fullname"]=="Ada LV426"&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["age"])==5&&Convert.ToInt32(card["related_to_killers"])==0;int encounters=0;
        foreach(Dictionary<string,object> e in (IEnumerable)card["encounters"]){cardOK &= Convert.ToInt32(e["appears_only_for_killer"])==-1&&Convert.ToInt32(e["has_clues_for_killer"])==0&&Convert.ToInt32(e["is_climax"])==0;encounters++;}
        Check(cardOK&&encounters==1,"Ada female robot sourcecard age5/LV426/ordinary noclue; not humanminor/changedcode/Unity appearance claim");
        Check(histories==3456&&controls==2,"Ada summary3456exact full histories/48passages122lines25answers/2controlledmovies/1actual033handoff");
    }
    static void MiloChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/milo_validation_routes.json");int histories=0,passages=0,answers=0,paired=0;
        var sourceScenes=new Dictionary<string,Dictionary<string,object>>();
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];sourceScenes[name]=scene;var row=(Dictionary<string,object>)rows[name];var covered=new HashSet<string>();var edges=new HashSet<string>();int count=0;
            foreach(Dictionary<string,object> route in (IEnumerable)scene["routes"]) {
                List<string> text,actions;var story=ReferenceRun(row,route,(string)scene["start"],MathieuGlobals(route["globals"]),out text,out actions);AngeliqueActions(route,actions);ReferenceCoverage(route,covered,edges);var path=new HashSet<string>(Strings(route["passages"]));
                bool ok=!story.can_continue&&story.GetChoices().Count==0&&!actions.Exists(a=>a.Contains("clue:")||a.Contains("fare:"))&&MathieuValue(story,"_Driver.Met.Milo")==0;
                bool intro=name=="203_milo_00a"||name=="203_milo_00b";
                ok &= actions.FindAll(a=>a=="travel: start").Count==(intro?1:0)&&actions.FindAll(a=>a=="travel: stop").Count==(intro?0:1)&&actions.FindAll(a=>a=="tip: low").Count==(intro?1:0);
                if(name=="203_milo_00a") {
                    ok &= text.Exists(t=>t.Contains("перешёл на ты"))&&text.Exists(t=>t.Contains("Шучу, амиго!"));
                    if(path.Contains("milo-00a-why"))ok &= text.Exists(t=>t.Contains("увидел твоё имя"))&&text.Exists(t=>t.Contains("Уссин"));
                    if(path.Contains("milo-00a-spy"))ok &= text.Exists(t=>t.Contains("на восьми языках"));
                }
                if(name=="203_milo_01") {
                    ok &= text.Exists(t=>t.Contains("весь день говорил по-испански"))&&text.Exists(t=>t.Contains("до конца года не доживу"))&&text.Exists(t=>t.Contains("оставить Кубу себе"))&&text.Exists(t=>t.Contains("помогаю правительствам"));
                    ok &= !text.Exists(t=>t.Contains("посольств"))&&actions.Contains("reveal: 203_01")&&!actions.Contains("reveal: 203_00");
                    if(path.Contains("milo-01-cuba-meeting"))ok &= text.Exists(t=>t.Contains("это буду я"));
                    if(path.Contains("milo-01-cuba-intro"))ok &= text.Exists(t=>t.Contains("всё ещё друг друга ненавидим"))&&text.Exists(t=>t.Contains("Мы ещё никогда так не дружили"));
                    if(path.Contains("milo-01-40s-silence"))ok &= text.Contains("Вы даже не пошевелились.");
                }
                if(name=="203_milo_02") {
                    int value=MathieuValue(story,"story");ok &= path.Contains("milo-02-exit-story")== (value==1)&&path.Contains("milo-02-end-story")== (value==1);
                    if(path.Contains("milo-02-snowden"))ok &= value==0&&path.Contains("milo-02-exit-storyno");
                    ok &= text.Contains("Вы отрицательно качаете головой.")&&text.Contains("Вы снова качаете головой.")&&text.Exists(t=>t.Contains("Славный малый"));
                    if(path.Contains("milo-02-wife"))ok &= text.Exists(t=>t.Contains("того, кто эту пулю получил"));
                    if(path.Contains("milo-02-exit-story"))ok &= text.Exists(t=>t.Contains("Не связывайте жизнь"));
                    if(path.Contains("milo-02-exit-storyno"))ok &= text.Exists(t=>t.Contains("забегаловку"));
                    ok &= !text.Exists(t=>t.Contains("погружени"))&&!text.Exists(t=>t.Contains("Лемонье"));
                }
                if(name=="203_milo_03") {
                    ok &= text.Exists(t=>t.Contains("У меня нет детей"))&&text.Exists(t=>t.Contains("Богдан"))&&text.Exists(t=>t.Contains("Бохдан"))&&text.Exists(t=>t.Contains("Суконными рядами"))&&text.Exists(t=>t.Contains("батончик"));
                    ok &= text.Exists(t=>t.Contains("педики"))==path.Contains("milo-03-silence");
                    if(path.Contains("milo-03-bogdan-03-intro"))ok &= text.Contains("МАЙЛО: «Очень».");
                    if(path.Contains("milo-03-death"))ok &= text.Exists(t=>t.Contains("Его убила полиция"))&&text.Exists(t=>t.Contains("выстрелили ему в спину"))&&text.Exists(t=>t.Contains("может быть оружие"));
                }
                Check(ok,"Milo "+name+" exact full history #"+(++count)+" source text/menu/state/actions and speaker context");histories++;
            }
            Check(covered.Count==Convert.ToInt32(scene["passages_covered"])&&edges.Count==Convert.ToInt32(scene["answers_covered"]),"Milo "+name+" all original passages/answer edges covered");passages+=covered.Count;answers+=edges.Count;
        }
        foreach(string introName in new string[]{"203_milo_00a","203_milo_00b"})foreach(string part in new string[]{"203_milo_01","203_milo_02","203_milo_03"}) {
            var intro=sourceScenes[introName];var segment=sourceScenes[part];Dictionary<string,object> before=null,after=null;
            foreach(Dictionary<string,object> r in (IEnumerable)intro["routes"]){before=r;break;}foreach(Dictionary<string,object> r in (IEnumerable)segment["routes"]){after=r;break;}
            List<string> firstText,firstActions,lastText,lastActions;var previous=ReferenceRun((Dictionary<string,object>)rows[introName],before,(string)intro["start"],null,out firstText,out firstActions);AngeliqueActions(before,firstActions);
            var obtained=new Dictionary<string,object>();foreach(string k in ((Dictionary<string,object>)after["globals"]).Keys)obtained[k]=MathieuValue(previous,k);
            var next=ReferenceRun((Dictionary<string,object>)rows[part],after,(string)segment["start"],MathieuGlobals(obtained),out lastText,out lastActions);AngeliqueActions(after,lastActions);var combined=new List<string>(firstActions);combined.AddRange(lastActions);
            Check(!previous.can_continue&&!next.can_continue&&combined.FindAll(a=>a=="travel: start").Count==1&&combined.FindAll(a=>a=="travel: stop").Count==1&&combined.FindAll(a=>a=="tip: low").Count==1,"Milo paired "+introName+" to "+part+" original intro-start/story-stop; not Unity selection proof");paired++;
        }
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2406.json");bool cardOK=(string)card["fullname"]=="Milo Reacher"&&Convert.ToInt32(card["gender"])==3&&Convert.ToInt32(card["age"])==55&&Convert.ToInt32(card["related_to_killers"])==0;int encounters=0;
        foreach(Dictionary<string,object> e in (IEnumerable)card["encounters"]){cardOK &= Convert.ToInt32(e["appears_only_for_killer"])==0&&Convert.ToInt32(e["has_clues_for_killer"])==0&&Convert.ToInt32(e["is_climax"])==0;encounters++;}
        Check(cardOK&&encounters==3,"Milo male55 sourcecard/threeencounters/noclue; appearance0 not Unity orchestration");
        Check(histories==982&&passages==60&&answers==48&&paired==6,"Milo summary982exact histories/60passages258lines48answers/6paired segments/4translatedcalls");
    }
    static List<string> ChantalDrain(PrompterStory story,int attempt) {
        var text=new List<string>();bool asleep=false;int n=0;
        while(story.can_continue) {
            if(++n>240)throw new Exception("Chantal segment loop");string line=story.GetNextLine();if(line==null)continue;text.Add(line);
            if(line=="Она улыбается, закрывая глаза.")asleep=true;
            if(line=="Пассажирка резко просыпается.")asleep=false;
            // Pace source sleep-fragment reading before the adjacent RNG links.
            // Vary ordinary reading delay; never override outcomes or seeds.
            if(asleep)System.Threading.Thread.Sleep(1+(attempt*37+n*73)%151);
        }
        return text;
    }
    static void ChantalCommands(Dictionary<string,object> blocks,List<string> path,List<string> actions) {
        var expected=new List<string>();foreach(string t in path)foreach(string raw in Strings(((Dictionary<string,object>)blocks[t])["commands"])) {string s=raw.Trim();expected.Add(s.Substring(2,s.Length-4).Trim());}
        AngeliqueActions(new Dictionary<string,object>{{"expected_actions",expected}},actions);
    }
    static void ChantalAmiraChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/chantal_amira_validation_routes.json");var ch=(Dictionary<string,object>)reference["chantal"];var blocks=(Dictionary<string,object>)ch["blocks"];var initial=TpsVars(ch["initial"]);var row=(Dictionary<string,object>)rows["205_chantal_01"];
        var policies=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> p in (IEnumerable)ch["prefixes"])policies.Add(p);
        var allowed=new HashSet<string>();foreach(object outcome in (IEnumerable)ch["random_outcomes"]) {var sentence=Strings(outcome).FindAll(t=>t.StartsWith("sleep-sentence"));allowed.Add(string.Join("/",sentence.ToArray()));}
        var observed=new HashSet<string>();var covered=new HashSet<string>();var edges=new HashSet<string>();int journeys=0;
        while(journeys<policies.Count) {
            var policy=policies[journeys%policies.Count];Dictionary<string,int> vars;List<string> actions;var story=TpsStart(row,"intro",initial,null,out vars,out actions);string start="intro";var answerList=new List<int>();foreach(object a in (IEnumerable)policy["answers"])answerList.Add(Convert.ToInt32(a));int nextAnswer=0;var path=new List<string>();var allText=new List<string>();
            while(true) {
                var text=ChantalDrain(story,journeys);allText.AddRange(text);var matched=TpsMatch(story,blocks,start,vars,text);vars=matched.Vars;path.AddRange(matched.Passages);
                if(matched.Menu=="END")break;
                if(nextAnswer>=answerList.Count)throw new Exception("Chantal source policy exhausted");int i=answerList[nextAnswer++];edges.Add(matched.Menu+"/"+i);var choices=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)((Dictionary<string,object>)blocks[matched.Menu])["choices"])choices.Add(c);story.ChooseAnswer(i);start=(string)choices[i]["link"];
            }
            ChantalCommands(blocks,path,actions);foreach(string t in path)covered.Add(t);string signature=string.Join("/",path.FindAll(t=>t.StartsWith("sleep-sentence")).ToArray());observed.Add(signature);
            bool ok=allowed.Contains(signature)&&nextAnswer==answerList.Count&&!story.can_continue&&story.GetChoices().Count==0&&path.Contains("drive-03");
            ok &= MathieuValue(story,"sleep")==Convert.ToInt32(((Dictionary<string,object>)policy["expected_before_sleep"])["sleep"]);
            ok &= actions.FindAll(a=>a=="travel: start").Count==1&&actions.FindAll(a=>a=="travel: stop").Count==1&&actions.FindAll(a=>a=="tip: high").Count==1&&!actions.Exists(a=>a.Contains("clue:")||a.Contains("fare:"))&&MathieuValue(story,"_Driver.Met.Chantal")==0;
            ok &= allText.Exists(t=>t.Contains("прав у меня больше нет"))==path.Contains("intro-explain")&&allText.Exists(t=>t.Contains("купюру"));
            if(path.Contains("sleep-sentence2D"))ok &= allText.Exists(t=>t.Contains("влюблена"));if(path.Contains("sleep-sentence3B"))ok &= allText.Exists(t=>t.Contains("кто режет себе язык"));
            Check(ok,"Chantal actual complete ride "+(++journeys)+" exact source segments/menus/endvars/actions; RNG="+observed.Count+"/81");
        }
        var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=50000000;string untouched=serializer.Serialize(row);int controls=0;
        foreach(object outcome in (IEnumerable)ch["random_outcomes"]) {
            var selected=Strings(outcome);var forced=new Dictionary<string,string>();for(int i=0;i+1<selected.Count;i++) {var original=(Dictionary<string,object>)blocks[selected[i]];if(((string)original["link"]).StartsWith("??"))forced[selected[i]]=selected[i+1];}
            var copy=new Dictionary<string,object>(row);var passages=new List<object>();foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"]) {
                var privatePassage=new Dictionary<string,object>(p);string title=(string)p["_title"];
                if(forced.ContainsKey(title)) {string link=(string)p["_link"];if(!link.StartsWith("??")||Array.IndexOf(link.Substring(2).Split('|'),forced[title])<0)throw new Exception("Chantal controlled edge outside original RNGlist");privatePassage["_link"]=forced[title];}
                passages.Add(privatePassage);
            }copy["_passages"]=passages;
            Dictionary<string,int> vars;List<string> actions;var story=TpsStart(copy,"sleep",initial,null,out vars,out actions);var text=Drain(story);var matched=TpsMatch(story,blocks,"sleep",vars,text);ChantalCommands(blocks,matched.Passages,actions);foreach(string title in matched.Passages)covered.Add(title);
            string actual=string.Join("/",matched.Passages.FindAll(t=>t.StartsWith("sleep-sentence")).ToArray());string wanted=string.Join("/",selected.FindAll(t=>t.StartsWith("sleep-sentence")).ToArray());
            Check(actual==wanted&&!story.can_continue&&story.GetChoices().Count==0&&MathieuValue(story,"sleep")==0,"Chantal controlled sleep combination "+(++controls)+" private allowed-edge copy, exact original text/state/actions; not actual random selection");
        }
        Check(journeys==60&&controls==81&&covered.Count==38&&edges.Count==19&&serializer.Serialize(row)==untouched,"Chantal all60actual answer histories and81controlledsleep combinations cover38passages100lines19answers; originalrow unchanged/not4860Cartesian pairs");
        foreach(int before in new int[]{2,3}) {
            Dictionary<string,int> vars;List<string> actions;var story=TpsStart(row,"drive-02",initial,new Dictionary<string,int>{{"sleep",before}},out vars,out actions);var text=ChantalDrain(story,before);var matched=TpsMatch(story,blocks,"drive-02",vars,text);ChantalCommands(blocks,matched.Passages,actions);
            Check(MathieuValue(story,"sleep")==4&&matched.Passages.Contains("drive-03")== (before==2)&&text.Exists(t=>t.Contains("коллег в таро"))== (before==2),"Chantal controlled strictsleep>3 afterincrement initial="+before+" private local copy; original unchanged");
        }
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2407.json");Check((string)card["fullname"]=="Chantal Lemonnier"&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["age"])==42&&Convert.ToInt32(card["related_to_killers"])==0,"Chantal female42 sourcecard/construction/no biography merge byfirstname; not Unity display");
        Check(journeys==60&&controls==81,"Chantal summary60actualhistories/81controlledsleepoutcomes/38blocks100lines19answers/1call actualRNGobserved="+observed.Count);
        var am=(Dictionary<string,object>)reference["amira"];row=(Dictionary<string,object>)rows["206_amira_01"];covered.Clear();edges.Clear();int histories=0;
        foreach(Dictionary<string,object> r in (IEnumerable)am["routes"]) {
            List<string> text,actions;var story=ReferenceRun(row,r,(string)am["start"],null,out text,out actions);AngeliqueActions(r,actions);ReferenceCoverage(r,covered,edges);var path=new HashSet<string>(Strings(r["passages"]));bool account=path.Contains("amira-01-story02-outro"),awful=path.Contains("amira-01-final-awful");
            bool ok=!story.can_continue&&story.GetChoices().Count==0&&MathieuValue(story,"story")== (account?1:0)&&path.Contains("amira-01-outro-story-hub")==account;
            ok &= awful==(path.Contains("amira-01-outro-story-good")||path.Contains("amira-01-outro-help"));
            ok &= actions.FindAll(a=>a=="travel: start").Count==1&&actions.FindAll(a=>a=="travel: stop").Count==1&&!actions.Exists(a=>a.Contains("tip:")||a.Contains("fare:")||a.Contains("clue:"))&&MathieuValue(story,"_Driver.Met.Amira")==0;
            ok &= text.Exists(t=>t.Contains("Улица де л’Аргонн"))&&text.Exists(t=>t.Contains("Я просто хочу домой"));
            if(account)ok &= MathieuValue(story,"china")==1&&MathieuValue(story,"game")==1&&text.Exists(t=>t.Contains("французская серия игр"))&&text.Exists(t=>t.Contains("Телефон на время перестаёт"))&&text.Exists(t=>t.Contains("то же, что только что вам"))&&text.Exists(t=>t.Contains("Я мусульманка"));
            else ok &= path.Contains("amira-01-story-silence02")&&!text.Exists(t=>t.Contains("Battles of Lost Legend"));
            if(path.Contains("amira-01-outro-story-good"))ok &= text.Exists(t=>t.Contains("захлопните пасть"));
            if(path.Contains("amira-01-outro-help"))ok &= text.Exists(t=>t.Contains("А вы как думаете"));
            ok &= text.Exists(t=>t.Contains("Мужчины у лавки выпрямляются"))==awful;
            Check(ok,"Amira exact complete history "+(++histories)+" full source text/menu/state/actions and player/speaker context");
        }
        Check(histories==666&&covered.Count==30&&edges.Count==25,"Amira all666finite histories cover30passages115Frenchlines25answers/two topicorders/no-story exit");
        card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2408.json");Check((string)card["fullname"]=="Amira Feghoul"&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["age"])==19&&Convert.ToInt32(card["related_to_killers"])==0,"Amira female19 sourcecard/religion from speech not inferredethnicity; no realcompany identification");
        Check(histories==666,"Amira summary666exact histories/30blocks115Frenchlines25answers/1call");
    }
    static void LolaRun(Dictionary<string,object> row,Dictionary<string,object> route,string label) {
        List<string> text,actions;var story=ReferenceRun(row,route,"intro",null,out text,out actions);AngeliqueActions(route,actions);var path=new HashSet<string>(Strings(route["passages"]));bool admission=path.Contains("frexit-admission"),english=path.Contains("french-english");
        bool ok=!story.can_continue&&story.GetChoices().Count==0&&MathieuValue(story,"admission")== (admission?1:0)&&path.Contains("outro-ahmed")==admission;
        ok &= path.Contains("frexit-irritated")!=admission&&path.Contains("frexit-admission-outro")==admission;
        ok &= actions.FindAll(a=>a=="travel: start").Count==1&&actions.FindAll(a=>a=="travel: stop").Count==1&&actions.FindAll(a=>a=="tip: low").Count==1&&actions.FindAll(a=>a=="tip: medium").Count==(english?1:0);
        ok &= actions.FindAll(a=>a.StartsWith("tip:")).Count==(english?2:1)&&!actions.Exists(a=>a.Contains("clue:")||a.Contains("fare:"))&&MathieuValue(story,"_Driver.Met.Lola")==0;
        ok &= text[0].Contains("сроке беременности")&&text.Exists(t=>t.StartsWith("ЛОЛА: «Просто сейчас"))&&!text.Exists(t=>t.StartsWith("ШАНТАЛЬ:"));
        ok &= text.Exists(t=>t.Contains("Она отрешённо глядит"))&&text.Exists(t=>t.Contains("Она… боевая"))&&text.Exists(t=>t.Contains("Ахмед настоял"))&&text.Exists(t=>t.Contains("паспорта ЕС"));
        if(path.Contains("england-pregnant"))ok &= text.Exists(t=>t.Contains("Семь месяцев"))&&text.Exists(t=>t.Contains("семнадцать"))&&text.Exists(t=>t.Contains("лимузин"))&&text.Exists(t=>t.Contains("самого крошечного VIP"));
        if(path.Contains("england-silent"))ok &= text.Exists(t=>t.Contains("Исламистана"))&&text.Exists(t=>t.Contains("Вы из хороших мусульман"));
        if(path.Contains("brexit-insane"))ok &= text.Exists(t=>t.Contains("Плевать мне было"))&&text.Exists(t=>t.Contains("Безумием не казалось"));
        if(path.Contains("brexit-outro-partner"))ok &= text.Exists(t=>t.Contains("его собака — ни слезинки"));
        ok &= text.Exists(t=>t.Contains("Ему сказали валить домой. А меня назвали грязной предательницей"))&&text.Exists(t=>t.Contains("Его мать ни слова по-английски, отец"));
        if(admission)ok &= text.Exists(t=>t.Contains("но и до тоже"))&&text.Exists(t=>t.Contains("чернокожей матери-одиночке"))&&text.Exists(t=>t.Contains("не сказала Ахмеду"));
        else ok &= text.Exists(t=>t.Contains("забыть об этом и жить дальше"))&&!text.Exists(t=>t.Contains("не сказала Ахмеду"));
        ok &= text.Exists(t=>t.Contains("чтобы ей открыли"))&&text.Exists(t=>t.Contains("мне здесь рады"));
        if(route.ContainsKey("english_guessed"))ok &= english==Convert.ToBoolean(route["english_guessed"])&&admission==Convert.ToBoolean(route["secret"]);
        Check(ok,"Lola "+label+" complete original text/menu/state/actions; speaker/family/idiom/tip/secret context");
    }
    static void LolaChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/lola_validation_routes.json");var scene=(Dictionary<string,object>)reference["scene"];var row=(Dictionary<string,object>)rows["207_lola_01"];var covered=new HashSet<string>();var edges=new HashSet<string>();int representatives=0,witnesses=0;var pairs=new HashSet<string>();
        foreach(Dictionary<string,object> route in (IEnumerable)scene["routes"]) {LolaRun(row,route,"representative #"+(++representatives));ReferenceCoverage(route,covered,edges);}
        Check(representatives==21&&covered.Count==40&&edges.Count==27,"Lola all40passages223lines27answers covered by21complete source menu-state routes, not all Cartesian histories");
        foreach(Dictionary<string,object> route in (IEnumerable)reference["joint_witnesses"]) {string pair=route["english_guessed"]+"/"+route["secret"]+"/"+route["parting"];pairs.Add(pair);LolaRun(row,route,"joint witness #"+(++witnesses)+" "+pair);}
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2409.json");Check((string)card["fullname"]=="Lola Hopkins"&&Convert.ToInt32(card["age"])==29&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["related_to_killers"])==0,"Lola female29 sourcecard; daughter/partner genders from speech, not names; no passport law/Unity claim");
        Check(representatives==21&&witnesses==12&&pairs.Count==12,"Lola summary21representatives/12jointwitnesses/40blocks223lines27answers/1call, source unchanged");
    }
    // Insert inside FullContextStoryChecks only after sealing the prior run.
    static PrompterStory MyrtilleRun(Dictionary<string,object> row,Dictionary<string,object> route,string number,List<DialogVariable> globals,out List<string> actions) {
        List<string> text;
        string start=number=="03"?"myrtille-03-start":"myrtille-"+number+"-intro";
        var story=ReferenceRun(row,route,start,globals,out text,out actions);
        AngeliqueActions(route,actions);
        if(story.can_continue||story.GetChoices().Count!=0)throw new Exception("Myrtille incomplete run "+number);
        if(text.Exists(t=>t.Contains("Миртилла")||t.Contains("Миртиллу")||t.Contains("Миртиллы")||t.Contains("Миртилле")))throw new Exception("Myrtille name drift");
        var path=new HashSet<string>(Strings(route["passages"]));
        bool intro=number=="00a"||number=="00b";
        if(actions.FindAll(a=>a=="travel: start").Count!=(intro?1:0)||actions.FindAll(a=>a=="travel: stop").Count!=(intro?0:1))throw new Exception("Myrtille travel boundary "+number);
        if(actions.FindAll(a=>a.StartsWith("tip:")).Count!=(intro?1:0)||intro&&!actions.Contains("tip: none"))throw new Exception("Myrtille original no-tip "+number);
        if(!intro) {
            bool correct=MathieuValue(story,"enigma_"+number)==1;
            var initial=(Dictionary<string,object>)route["globals"];
            int good=Convert.ToInt32(initial["_enigmaGoodAnswer"]),bad=Convert.ToInt32(initial["_enigmaBadAnswer"]),asked=Convert.ToInt32(initial["_enigmaAsked"]);
            bool deliberate=number=="01"&&path.Contains("myrtille-01-fake");
            if(MathieuValue(story,"_enigmaAsked")!=asked+1||MathieuValue(story,"_enigmaGoodAnswer")!=good+(correct?1:0)||MathieuValue(story,"_enigmaBadAnswer")!=bad+(!correct&&!deliberate?1:0))throw new Exception("Myrtille answer counter or fake01exception "+number);
            bool free=actions.Contains("fare: 0");
            if(!correct&&!free||actions.FindAll(a=>a.StartsWith("fare:")).Count!=(free?1:0))throw new Exception("Myrtille original free/paid branch "+number);
            if(actions.FindAll(a=>a.StartsWith("reveal:")).Count!=(correct?1:0)||correct&&!actions.Contains("reveal: 208_"+number))throw new Exception("Myrtille reveal "+number);
            if(number=="02"&&MathieuValue(story,"saucisson")!=(path.Contains("myrtille-02-answer-fake")?1:0))throw new Exception("Myrtille salami flag");
        }
        return story;
    }
    static void MyrtilleChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/myrtille_validation_routes.json");
        if(Convert.ToBoolean(reference["draft_only"]))throw new Exception("Myrtille draft reference is not approved editorial input");
        var flags=Strings(reference["counter_flags"]);int histories=0,riddles=0,profiles=0,passages=0,answers=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"],number=(string)scene["number"];var row=(Dictionary<string,object>)rows[name];
            var covered=new HashSet<string>();var edges=new HashSet<string>();int count=0;
            foreach(Dictionary<string,object> route in (IEnumerable)scene["routes"]) {
                List<string> actions;MyrtilleRun(row,route,number,MathieuGlobals(route["globals"]),out actions);ReferenceCoverage(route,covered,edges);
                Check(true,"Myrtille "+name+" exact complete source history #"+(++count)+" text/menu/state/actions; payment and riddle ownership");histories++;
            }
            Check(covered.Count==Convert.ToInt32(scene["passages_covered"])&&edges.Count==Convert.ToInt32(scene["answers_covered"]),"Myrtille "+name+" complete original passage/answer coverage");
            passages+=covered.Count;answers+=edges.Count;if(number=="00b")profiles+=count;else if(number!="00a")riddles+=count;
        }
        int handoffs=0;var resultProfiles=new HashSet<string>();
        foreach(Dictionary<string,object> witness in (IEnumerable)reference["handoffs"]) {
            var obtained=new Dictionary<string,object>();var combined=new List<string>();int steps=0;
            foreach(Dictionary<string,object> step in (IEnumerable)witness["steps"]) {
                string number=(string)step["number"];var route=(Dictionary<string,object>)step["route"];
                List<string> emitted;var story=MyrtilleRun((Dictionary<string,object>)rows["208_myrtille_"+number],route,number,steps==0?null:MathieuGlobals(obtained),out emitted);combined.AddRange(emitted);
                foreach(string flag in flags)obtained[flag]=MathieuValue(story,flag);
                steps++;
            }
            bool ok=true;foreach(var p in (Dictionary<string,object>)witness["counters"])ok &= Convert.ToInt32(obtained[p.Key])==Convert.ToInt32(p.Value);
            int depth=Convert.ToInt32(witness["depth"]);
            ok &= combined.FindAll(a=>a=="travel: start").Count==depth+1&&combined.FindAll(a=>a=="travel: stop").Count==depth&&combined.FindAll(a=>a=="tip: none").Count==depth+1;
            string signature=obtained[flags[0]]+"/"+obtained[flags[1]]+"/"+obtained[flags[2]];resultProfiles.Add(signature);
            Check(ok,"Myrtille actual serial counter handoff #"+(++handoffs)+" "+signature+" with intervening repeatintros; test order, not Unity scheduling");
        }
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2410.json");int encounters=0;
        bool cardOK=(string)card["fullname"]=="Myrtille Thevenou"&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["age"])==53&&Convert.ToInt32(card["related_to_killers"])==0;
        foreach(Dictionary<string,object> e in (IEnumerable)card["encounters"]) {cardOK &= Convert.ToInt32(e["has_clues_for_killer"])==0&&Convert.ToInt32(e["is_climax"])==0;encounters++;}
        Check(cardOK&&encounters==4,"Myrtille woman53/fourencounters sourcecard; Simone friend like sister, not bloodrelation");
        var firstCalls=Strings(((Dictionary<string,object>)rows["208_myrtille_00a"])["_user_datas"]);var repeatCalls=Strings(((Dictionary<string,object>)rows["208_myrtille_00b"])["_user_datas"]);
        Check(firstCalls.Count==1&&repeatCalls.Count==3&&firstCalls[0]=="passengercall: «Эй, друг!»"&&repeatCalls[0]=="passengercall: «Как обычно! Ха-ха!»"&&repeatCalls[1]=="passengercall: «В приют в Монтрёй, спасибо»."&&repeatCalls[2]=="passengercall: «Мне в Монтрёй…»","Myrtille all four useful passenger invitations retain source prefixes and translated contents");
        Check(histories==124&&riddles==99&&profiles==24&&handoffs==24&&resultProfiles.Count==24&&passages==89&&answers==42,"Myrtille summary99finite histories/24repeatprofiles/24serialhandoffs/89passages370lines42answers/4calls; no forced links/RNG");
    }

    static PrompterStory XavierRun(Dictionary<string,object> row,Dictionary<string,object> route,string name,out List<string> text,out List<string> actions,List<DialogVariable> globals=null,bool direct=false) {
        var story=ReferenceRun(row,route,(string)route["start"],globals??MathieuGlobals(route["globals"]),out text,out actions);AngeliqueActions(route,actions);
        var path=new HashSet<string>(Strings(route["passages"]));bool first=name.EndsWith("01"),second=name.EndsWith("02");
        if(story.can_continue||story.GetChoices().Count!=0)throw new Exception("Xavier incomplete route "+name);
        if(actions.Exists(a=>a.StartsWith("fare:")||a.StartsWith("clue:")||a.StartsWith("destination:"))||MathieuValue(story,"_Driver.Met.Xavier")!=0)throw new Exception("Xavier invented fare/clue/destination/meetingflag");
        bool low=first&&path.Contains("xavier-01-drive02-name-horrible");
        int time=first||second?0:MathieuValue(story,"time"),accident=first||second?0:MathieuValue(story,"accident");
        bool high=!first&&!second&&time>1&&accident==1;var tips=actions.FindAll(a=>a.StartsWith("tip:"));
        if(tips.Count!=(direct?0:1)+(low?1:0)+(high?1:0)||!direct&&tips[0]!="tip: medium"||low&&!tips.Contains("tip: low")||high&&!tips.Contains("tip: high"))throw new Exception("Xavier source tip sequence "+name);
        if(!direct) {
            int expected=first||second?1:1+(path.Contains("light-01-stop")?1:0)+(path.Contains("light-02-stop")?1:0);
            if(actions.FindAll(a=>a=="travel: start").Count!=expected||actions.FindAll(a=>a=="travel: stop").Count!=expected)throw new Exception("Xavier source travel stops/starts "+name);
        }
        if(first) {
            if(!text[0].Contains("заброшенного склада")||!text.Exists(t=>t.Contains("проверяет пульс"))||!text.Exists(t=>t.StartsWith("КСАВЬЕ: «Нет. Бэтмен!")))throw new Exception("Xavier restored source start/pulse/intentional Batman correction");
        } else if(second) {
            bool investigation=MathieuValue(story,"_freeroam")==0;
            if(path.Contains("xavier-02-story03-killer")!=investigation||text.Contains("Он или она застали вас врасплох.")!=investigation)throw new Exception("Xavier unknown attacker case/free-roam boundary");
        } else {
            bool early=time>1;
            if(path.Contains("bank-explosion-early")!=early||path.Contains("bank-explosion-late")==early||path.Contains("bank-explosion-early-accident")!=(early&&accident==1))throw new Exception("Xavier strict time>1 branch");
            if(!direct&&!(time==0&&accident==0||time==1&&accident==0||time==2&&accident==1||time==3&&accident==0))throw new Exception("Xavier unattainable ordinary time/accident pair");
            if(!direct&&text[0].Contains("пассажирке"))throw new Exception("Xavier foreign female intro");
            if(text.Exists(t=>t.Contains("Он протягивает вам правую руку"))==early)throw new Exception("Xavier FR right-hand branch preserved");
            if(!direct) {
                bool radio=MathieuValue(story,"radio")==1,tell=MathieuValue(story,"tell")==1;
                if(path.Contains("intro-radio-on")!=radio||path.Contains("drive-radio-on")!=radio||path.Contains("drive-time-tell")!=tell)throw new Exception("Xavier joint radio/tell state");
            }
        }
        return story;
    }
    static void XavierChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/xavier_validation_routes.json");int routes=0,passages=0,answers=0,metadata=0;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"]) {
            string name=(string)scene["scene"];var row=(Dictionary<string,object>)rows[name];var covered=new HashSet<string>();var edges=new HashSet<string>();int count=0;
            foreach(Dictionary<string,object> route in (IEnumerable)scene["routes"]) {
                List<string> text,actions;XavierRun(row,route,name,out text,out actions);ReferenceCoverage(route,covered,edges);
                Check(true,"Xavier "+name+" complete representative source history #"+(++count)+" exact text/menus/state/actions; source gender and conditional endings");routes++;
            }
            Check(covered.Count==Convert.ToInt32(scene["passages_covered"])&&edges.Count==Convert.ToInt32(scene["answers_covered"]),"Xavier "+name+" all normallyreachable source passages/answer edges");passages+=covered.Count;answers+=edges.Count;
            var actual=Strings(row["_user_datas"]);var wanted=Strings(scene["user_datas"]);bool same=actual.Count==8&&wanted.Count==8;
            for(int i=0;i<actual.Count&&same;i++)same &= actual[i]==wanted[i]&&(i<7?actual[i].StartsWith("anim-link:"):actual[i].StartsWith("passengercall:"));
            Check(same,"Xavier "+name+" seven literal animation definitions and translated useful call at index7");metadata++;
        }
        int tips=0;foreach(Dictionary<string,object> route in (IEnumerable)reference["tip_witnesses"]) {
            List<string> t,a;XavierRun((Dictionary<string,object>)rows["210_xavier_01"],route,"210_xavier_01",out t,out a);
            Check(a.Contains("tip: low")==Convert.ToBoolean(route["low_tip"]),"Xavier joint name-response/tip/parting witness #"+(++tips));
        }
        int timings=0;var outcomes=new HashSet<string>();
        foreach(Dictionary<string,object> route in (IEnumerable)reference["timing_witnesses"]) {
            List<string> t,a;var story=XavierRun((Dictionary<string,object>)rows["210_xavier_03"],route,"210_xavier_03",out t,out a);int radio=MathieuValue(story,"radio"),tell=MathieuValue(story,"tell"),time=MathieuValue(story,"time"),accident=MathieuValue(story,"accident");outcomes.Add(time+"/"+accident);
            Check(radio==(Convert.ToBoolean(route["radio"])?1:0)&&tell==(Convert.ToBoolean(route["tell"])?1:0),"Xavier joint radio/tell/time/accident/parting witness #"+(++timings)+" "+time+"/"+accident);
        }
        var direct=(Dictionary<string,object>)reference["unlinked_direct"];List<string> directText,directActions;XavierRun((Dictionary<string,object>)rows["210_xavier_03"],direct,"210_xavier_03",out directText,out directActions,null,true);
        Check(Strings(direct["passages"])[0]=="drive-getrid"&&directText[0].Contains("От бомб надо избавляться"),"Xavier original unlinked drive-getrid direct entry and complete tail, no added link");
        int serials=0;
        foreach(Dictionary<string,object> serial in (IEnumerable)reference["serial_rides"]) {
            int supplied=Convert.ToInt32(serial["freeroam"]);var globals=new Dictionary<string,object>{{"_freeroam",supplied}};bool same=true;int stages=0;PrompterStory previous=null;
            foreach(Dictionary<string,object> step in (IEnumerable)serial["steps"]) {
                string name=(string)step["name"];List<string> t,a;previous=XavierRun((Dictionary<string,object>)rows[name],(Dictionary<string,object>)step["route"],name,out t,out a,MathieuGlobals(globals));same &= MathieuValue(previous,"_freeroam")==supplied;globals["_freeroam"]=MathieuValue(previous,"_freeroam");stages++;
            }
            Check(same&&stages==3&&previous!=null,"Xavier serial trilogy #"+(++serials)+" external freeroam="+supplied+"/ending="+serial["ending"]+"; no invented meeting setter/Unity ordering claim");
        }
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2411.json");int encounters=0;bool ok=(string)card["fullname"]=="Xavier Turcev"&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["age"])==0&&Convert.ToInt32(card["related_to_killers"])==0;
        foreach(Dictionary<string,object> e in (IEnumerable)card["encounters"]){ok &= Convert.ToInt32(e["has_clues_for_killer"])==0&&Convert.ToInt32(e["is_climax"])==0;encounters++;}
        Check(ok&&encounters==3,"Xavier unchanged sourcecard0/age0 contradiction, male from explicit EN/FR not inferred infant/female; no Unity selection claim");
        Check(routes==96&&passages==85&&answers==73&&metadata==3&&tips==8&&timings==24&&outcomes.SetEquals(new string[]{"0/0","1/0","2/1","3/0"})&&serials==8,"Xavier summary96representatives/8tip/24timing/8serial/85normal+1unlinked/86blocks367lines73answers/3calls21animationdefs");
    }

    static PrompterStory RoksanaRun(Dictionary<string,object> row,Dictionary<string,object> route,List<DialogVariable> globals,out List<string> text,out List<string> actions) {
        var story=ReferenceRun(row,route,(string)route["start"],globals,out text,out actions);AngeliqueActions(route,actions);var path=new HashSet<string>(Strings(route["passages"]));int met=Convert.ToInt32(((Dictionary<string,object>)route["globals"])["_Driver.Met.Ludwig"]);bool known=met==1,money=path.Contains("roksana-exit-money");
        if(story.can_continue||story.GetChoices().Count!=0||MathieuValue(story,"_Driver.Met.Ludwig")!=met||MathieuValue(story,"_Driver.Met.Roksana")!=0)throw new Exception("Roksana meeting/end state");
        if(path.Contains("ludwig-intro-known")!=known||path.Contains("ludwig-chat-known")!=known||path.Contains("roksana-intro-ludwigknown")!=known||path.Contains("roksana-secondadvice-ludwigknown")!=known)throw new Exception("Roksana source Ludwig memory condition");
        if(path.Contains("ludwig-intro-unknown")==known||path.Contains("roksana-secondadvice-ludwigunknown")==known)throw new Exception("Roksana future meeting versus remembered ghost");
        if(actions.FindAll(a=>a.StartsWith("fare:")).Count!=1||!actions.Contains("fare: 0")||actions.Exists(a=>a.StartsWith("travel:")||a.StartsWith("destination:")||a.StartsWith("clue:")))throw new Exception("Roksana unchanged zero fare/no travel/clue");
        var tips=actions.FindAll(a=>a.StartsWith("tip:"));if(tips.Count!=(money?2:1)||tips[0]!="tip: low"||money&&tips[1]!="tip: none")throw new Exception("Roksana demand payment source low/none");
        if(text.Contains("Тут? Она имеет в виду Францию?")!=money||text.Exists(t=>t.Contains("Никогда не видела"))!=path.Contains("ludwig-chat-known-seehim"))throw new Exception("Roksana FR narrator thought/own ghost sighting denial");
        if(!text.Contains("РОКСАНА: «Не злите его. Слушайте. Помогайте ему. Подружитесь с ним».")||text.Exists(t=>t.Contains("на целую вечность"))==known)throw new Exception("Roksana original contradictory friendship/eternal Underworld warning");
        if(!text.Contains("Течёт.")||!text.Contains("По вашим венам.")||!text.Contains("…"))throw new Exception("Roksana source fear metaphor and final ellipsis");
        return story;
    }
    static void RoksanaChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/roksana_validation_routes.json");var row=(Dictionary<string,object>)rows["214_roksana_01"];var covered=new HashSet<string>();var edges=new HashSet<string>();int journeys=0,unknown=0,known=0;
        foreach(Dictionary<string,object> route in (IEnumerable)reference["routes"]) {
            List<string> t,a;RoksanaRun(row,route,MathieuGlobals(route["globals"]),out t,out a);ReferenceCoverage(route,covered,edges);
            int met=Convert.ToInt32(((Dictionary<string,object>)route["globals"])["_Driver.Met.Ludwig"]);if(met==0)unknown++;else known++;
            Check(true,"Roksana complete finite original history #"+(++journeys)+" MetLudwig="+met+" exact text/menu/state/actions; claims not independently confirmed facts");
        }
        Check(journeys==144&&unknown==72&&known==72&&covered.Count==28&&edges.Count==19,"Roksana all144finite histories/28passages83lines19answers and empty-block English menu icons");
        int pairs=0;
        foreach(Dictionary<string,object> pair in (IEnumerable)reference["pairs"]) {
            var before=(Dictionary<string,object>)pair["before"];List<string> t,a;var first=ReferenceRun((Dictionary<string,object>)rows["009_ludwig_00a"],before,(string)before["start"],MathieuGlobals(before["globals"]),out t,out a);AngeliqueActions(before,a);
            int met=MathieuValue(first,"_Driver.Met.Ludwig");var after=(Dictionary<string,object>)pair["after"];var next=RoksanaRun(row,after,new List<DialogVariable>{new DialogVariable("_Driver.Met.Ludwig",met)},out t,out a);
            Check(met==1&&MathieuValue(next,"_Driver.Met.Ludwig")==1&&Strings(after["passages"]).Contains("ludwig-chat-known-seehim"),"Roksana actual completed009intro meeting flag to full known214 #"+(++pairs)+";three firstanswers/twopaymentrequests, not Unity order");
        }
        var actual=Strings(row["_user_datas"]);var wanted=Strings(reference["user_datas"]);
        Check(actual.Count==1&&wanted.Count==1&&actual[0]==wanted[0]&&actual[0].StartsWith("passengercall: «Я должна с вами поговорить."),"Roksana one useful translated invitation with unchanged source prefix");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2412.json");
        Check((string)card["fullname"]=="Roksana Krilova"&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["age"])==39&&Convert.ToInt32(card["related_to_killers"])==0,"Roksana female39 sourcecard/chasseuse, accent not nationality/ghost identity proof");
        Check(journeys==144&&pairs==6,"Roksana summary144finite histories/6actual009introflaghandoffs/28blocks83lines19answers/1call;source contradiction preserved");
    }

    static PrompterStory AlphRun(Dictionary<string,object> row,Dictionary<string,object> route,out List<string> text,out List<string> actions) {
        var story=ReferenceRun(row,route,(string)route["start"],null,out text,out actions);AngeliqueActions(route,actions);
        var p=new HashSet<string>(Strings(route["passages"]));bool hypnotized=p.Contains("alph-01-question02-who-outro"),paid=p.Contains("alph-01-drive-no-money");
        if(story.can_continue||story.GetChoices().Count!=0||MathieuValue(story,"hypnosis")!=(hypnotized?1:0)||p.Contains("alph-01-question03-hypnosis")!=hypnotized)throw new Exception("Alph original hypnosis condition/end");
        if(!text.Contains("Вы вдруг замечаете, что не можете говорить. Что-то застряло в горле.")||!text.Contains("Вы всё ещё парализованы, но не паникуете.")||!text.Contains("Через минуту ваши пальцы отпускают руль. Вы больше не парализованы."))throw new Exception("Alph universal voice loss/paralysis independent of local hypnosis");
        if(!text.Contains("АЛЬФ: «Спасибо, У…»")||text.Exists(t=>t.Contains("Уссин")||t.Contains("Эйч")))throw new Exception("Alph partial initial must not reveal full driver name");
        if(actions.FindAll(a=>a=="money: 50").Count!=(paid?1:0)||actions.FindAll(a=>a.StartsWith("money:")).Count!=(paid?1:0))throw new Exception("Alph returned francs versus single accepted euro payment");
        if(actions.FindAll(a=>a=="travel: start").Count!=1||actions.FindAll(a=>a=="travel: stop").Count!=1||actions.FindAll(a=>a=="reveal: 217_00").Count!=1||actions.Exists(a=>a.StartsWith("tip:")||a.StartsWith("fare:")||a.StartsWith("clue:")||a.StartsWith("destination:"))||MathieuValue(story,"_Driver.Met.Alph")!=0)throw new Exception("Alph source travel/reveal/no invented tip/fare/meeting");
        return story;
    }
    static void AlphChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/alph_validation_routes.json");var row=(Dictionary<string,object>)rows["217_alph_01"];var covered=new HashSet<string>();var edges=new HashSet<string>();var matrix=new HashSet<string>();int journeys=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> t,a;var story=AlphRun(row,r,out t,out a);ReferenceCoverage(r,covered,edges);int third=-1;
            foreach(object edge in (IEnumerable)r["edges"]) {var e=new List<object>();foreach(object x in (IEnumerable)edge)e.Add(x);if((string)e[0]=="alph-01-question03-cont")third=Convert.ToInt32(e[1]);}
            matrix.Add((a.Contains("money: 50")?1:0)+"/"+MathieuValue(story,"hypnosis")+"/"+third);
            Check(true,"Alph complete finite original history #"+(++journeys)+" exact text/menu/state/actions and universal ending");
        }
        Check(journeys==756&&covered.Count==34&&edges.Count==27,"Alph all756finite histories/34blocks152lines27answers");
        Check(matrix.Count==12&&!matrix.Contains("0/0/-1"),"Alph all12actual payment/hypnosis/thirdanswer combinations without forced state");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2413.json");var proof=ReferenceFile("localization/contexts/217_alph_enum_validation.json");string actualHash;
        using(var hash=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead((string)proof["assembly"]))actualHash=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        Check((string)card["fullname"]=="Alph"&&Convert.ToInt32(card["gender"])==2&&Convert.ToInt32(card["ethnicity"])==2&&Convert.ToInt32(card["age"])==99&&Convert.ToInt32(proof["group_gender_value"])==2&&Convert.ToInt32(proof["animal_ethnicity_value"])==2&&actualHash==(string)proof["assembly_sha256"],"Alph live assembly hash matches enum evidence: group/animal are categories, no invented species/count/biological gender");
        var actual=Strings(row["_user_datas"]);var expected=Strings(reference["user_datas"]);
        Check(actual.Count==1&&expected.Count==1&&actual[0]==expected[0]&&actual[0].Contains("Я еду в шестнадцатый округ"),"Alph translated invitation preserves original prefix and destination");
        Check(journeys==756&&matrix.Count==12,"Alph summary756finite histories/12joint signatures/34blocks152lines27answers/1call");
    }
    static string NathalieProfile(PrompterStory story) {
        var parts=new List<string>();foreach(string key in new string[]{"name-alfred","name-jeanpaul","name-redwane","name-ali","job-engineer","job-teacher","job-spy","job-chef","kids"})parts.Add(MathieuValue(story,key).ToString());return string.Join("/",parts.ToArray());
    }
    static PrompterStory NathalieRun(Dictionary<string,object> row,Dictionary<string,object> r,out List<string> text,out List<string> actions) {
        var story=ReferenceRun(row,r,(string)r["start"],null,out text,out actions);AngeliqueActions(r,actions);var p=new HashSet<string>(Strings(r["passages"]));
        if(story.can_continue||story.GetChoices().Count!=0||MathieuValue(story,"_Driver.Met.NATHALIE")!=1||MathieuValue(story,"_Driver.Met.Nathalie")!=0)throw new Exception("Nathalie exact uppercase source meeting flag/end");
        int names=0,jobs=0;string alias=null,job=null;
        foreach(string k in new string[]{"alfred","jeanpaul","redwane","ali"}) {int v=MathieuValue(story,"name-"+k);if(v!=0&&v!=1)throw new Exception("Nathalie nonbinary name flag");names+=v;if(v==1)alias=k;}
        foreach(string k in new string[]{"engineer","teacher","spy","chef"}) {int v=MathieuValue(story,"job-"+k);if(v!=0&&v!=1)throw new Exception("Nathalie nonbinary job flag");jobs+=v;if(v==1)job=k;}
        if(names>1||jobs>1||jobs>names||p.Contains("nathalie-01-storyB-outro-kids")!=(MathieuValue(story,"kids")==1))throw new Exception("Nathalie onehot false identities/jobs and source kids condition");
        if(actions.FindAll(a=>a=="travel: start").Count!=1||actions.FindAll(a=>a=="travel: stop").Count!=1||actions.FindAll(a=>a=="fare: 0").Count!=1||actions.FindAll(a=>a=="reveal: 219_00").Count!=1||actions.Exists(a=>a.StartsWith("tip:")||a.StartsWith("money:")||a.StartsWith("clue:")||a.StartsWith("destination:")))throw new Exception("Nathalie original one travel pair/zero fare/reveal/no invented rewards");
        if(p.Contains("nathalie-01-HerStoryC-03-same")) {
            foreach(string k in new string[]{"engineer","teacher","spy","chef"})if(p.Contains("nathalie-01-HerStoryC-03-same-"+k)!=(job==k))throw new Exception("Nathalie later job callback mismatch");
            if(p.Contains("nathalie-01-HerStoryC-03-same-outroA")!=(job==null))throw new Exception("Nathalie true colleague callback");
        }
        if(p.Contains("nathalie-01-HerStoryC-04")) {
            foreach(string k in new string[]{"alfred","jeanpaul","redwane","ali","houssine"})if(p.Contains("nathalie-01-thankyou-"+k)!=(k==(alias??"houssine")))throw new Exception("Nathalie later alias callback mismatch");
            if((alias==null||alias=="alfred")&&!text.Contains("НАТАЛИ: «Мне было приятно… Альфред»."))throw new Exception("Nathalie original wrong Alfred in true-name branch must stay");
            if(alias=="jeanpaul"&&(!text.Contains("НАТАЛИ: «Мне было приятно… Жан-Жак».")||!text.Contains("«Кажется… я назвался Жан-Полем».")))throw new Exception("Nathalie Jean-Jacques source slip and correction");
        }
        return story;
    }
    static void NathalieChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/nathalie_validation_routes.json");var scene=(Dictionary<string,object>)reference["scene"];var row=(Dictionary<string,object>)rows["219_nathalie_01"];var covered=new HashSet<string>();var edges=new HashSet<string>();var profiles=new HashSet<string>();int journeys=0,pairs=0;
        foreach(Dictionary<string,object> r in (IEnumerable)scene["routes"]) {
            List<string> t,a;var story=NathalieRun(row,r,out t,out a);ReferenceCoverage(r,covered,edges);profiles.Add(NathalieProfile(story));
            Check(true,"Nathalie complete representative original route #"+(++journeys)+" exact text/menu/state/actions; equivalent prefixes merged");
        }
        Check(journeys==341&&covered.Count==61&&edges.Count==33,"Nathalie all61blocks205lines33answers covered by341representative routes");
        var joint=new HashSet<string>();foreach(Dictionary<string,object> r in (IEnumerable)reference["joint_witnesses"]) {
            List<string> t,a;var story=NathalieRun(row,r,out t,out a);joint.Add(NathalieProfile(story));
            Check(Strings(r["passages"]).Contains("nathalie-01-HerStoryC-03-same"),"Nathalie actual joint alias/job/kids to late keepdriving and thanks #"+(++pairs));
        }
        Check(profiles.Count==42&&joint.Count==42&&profiles.SetEquals(joint),"Nathalie all42attainable family/falseidentity profiles have ordinary full-route late callbacks");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2414.json");
        Check((string)card["fullname"]=="Nathalie Pingeot"&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["age"])==74&&Convert.ToInt32(card["driver"])==0,"Nathalie source card74 versus spoken72 kept; scene woman driving is not overwritten by driver0");
        Check(Strings(row["_user_datas"]).Count==0&&Strings(scene["user_datas"]).Count==0,"Nathalie no source invitation: none invented");
        Check(journeys==341&&pairs==42,"Nathalie summary341representative routes/42joint witnesses/61blocks205lines33answers/0calls; source wrong names retained");
    }

    static PrompterStory JimRun(Dictionary<string,object> row,Dictionary<string,object> r,out List<string> text,out List<string> actions) {
        var story=ReferenceRun(row,r,(string)r["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);var p=new HashSet<string>(Strings(r["passages"]));
        if(story.can_continue||story.GetChoices().Count!=0||MathieuValue(story,"_Driver.Met.Jim")!=0||MathieuValue(story,"_GOT_KILLED")!=0)throw new Exception("Jim source end/no invented meeting or death state");
        if(p.Contains("jim-bad-end")&&p.Contains("jim-end-attack")!=(MathieuValue(story,"annoyed")>2))throw new Exception("Jim strict source annoyed threshold");
        if(p.Contains("jim-end-attack")&&(!text.Contains("На шее — ни раны.")||!text.Contains("В кармане несколько монет — чаевые?")||!text.Contains("…")))throw new Exception("Jim source attack aftermath/uncertain tip/ellipsis");
        if(p.Contains("jim-end-restaurant-drink")&&!text.Contains("ДЖИМ: «Нашему другу пива, пожалуйста. Аббатского. Трипель, если есть»."))throw new Exception("Jim abbey tripel not a brand or three beers");
        if(p.Contains("jim-end-restaurant-coffee")&&!text.Contains("ДЖИМ: «Кофе для месье. Очень крепкий. Без сахара»."))throw new Exception("Jim exact coffee preference restored");
        if(actions.Exists(a=>a.StartsWith("tip:")||a.StartsWith("fare:")||a.StartsWith("money:")||a.StartsWith("clue:")||a.StartsWith("destination:")))throw new Exception("Jim narrated reward is not new command");
        if(actions.FindAll(a=>a=="travel: start").Count!=(p.Contains("jim-drive")?1:0)||actions.FindAll(a=>a=="travel: stop").Count!=(p.Contains("jim-end-restaurant")?1:0))throw new Exception("Jim original travel command asymmetry");
        return story;
    }
    static Dictionary<string,object> PrivateVariables(Dictionary<string,object> row,Dictionary<string,object> values) {
        var copy=new Dictionary<string,object>(row);var variables=new List<object>();
        foreach(Dictionary<string,object> v in (IEnumerable)row["_variables"]) {var item=new Dictionary<string,object>(v);if(values.ContainsKey((string)v["_name"]))item["_value"]=values[(string)v["_name"]];variables.Add(item);}copy["_variables"]=variables;return copy;
    }
    static void JimChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/jim_validation_routes.json");var row=(Dictionary<string,object>)rows["221_jim_01"];var seen=new HashSet<string>();var edges=new HashSet<string>();int n=0,controls=0,pairs=0;var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=50000000;string original=serializer.Serialize(row);
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> t,a;var story=JimRun(row,r,out t,out a);ReferenceCoverage(r,seen,edges);int met=Convert.ToInt32(((Dictionary<string,object>)r["globals"])["_Driver.Met.Camille"]);var p=new HashSet<string>(Strings(r["passages"]));
            if(MathieuValue(story,"_Driver.Met.Camille")!=met||p.Contains("jim-end-attack"))throw new Exception("Jim source memory flag or unreachable attack");
            if(p.Contains("jim-heat-taxi")&&(p.Contains("jim-camille")!=(met==1)||p.Contains("jim-heat-taxi02")!=(met==0)))throw new Exception("Jim source Camille memory menus");
            Check(true,"Jim complete finite ordinary history #"+(++n)+" exact lines/menus/state/actions; no ordinary attack");
        }
        Check(n==728&&seen.Count==45&&edges.Count==47&&!seen.Contains("jim-end-attack")&&!seen.Contains("jim-end-restaurant-nothing"),"Jim728ordinary histories cover45blocks47answers;two original fragments unreachable");
        foreach(Dictionary<string,object> r in (IEnumerable)reference["controls"]) {
            var copy=PrivateVariables(row,(Dictionary<string,object>)r["globals"]);List<string> t,a;JimRun(copy,r,out t,out a);ReferenceCoverage(r,seen,edges);
            Check(true,"Jim controlled original block entry #"+(++controls)+" annoyed2escape/3attack or no-drink fragment; not ordinary path");
        }
        Check(controls==3&&seen.Count==47&&serializer.Serialize(row)==original,"Jim all47blocks plus private controls without source row/link edits");
        foreach(Dictionary<string,object> pair in (IEnumerable)reference["pairs"]) {
            var before=(Dictionary<string,object>)pair["before"];List<string> t,a;var first=ReferenceRun((Dictionary<string,object>)rows["053_camille_01"],before,(string)before["start"],null,out t,out a);AngeliqueActions(before,a);int met=MathieuValue(first,"_Driver.Met.Camille");
            var after=(Dictionary<string,object>)pair["after"];var globals=new Dictionary<string,object>((Dictionary<string,object>)after["globals"]);globals["_Driver.Met.Camille"]=met;var route=new Dictionary<string,object>(after);route["globals"]=globals;var next=JimRun(row,route,out t,out a);
            Check(met==1&&MathieuValue(next,"_Driver.Met.Camille")==1&&Strings(route["passages"]).Contains("jim-camille02"),"Jim actual completed053entrepreneur to221memory and coffee/beer #"+(++pairs)+";not Unity schedule proof");
        }
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2415.json");
        Check((string)card["fullname"]=="Jim M."&&Convert.ToInt32(card["gender"])==3&&Convert.ToInt32(card["age"])==87,"Jim male87sourcecard versus perceived30s; no invented celebrity/vampire identity");
        var actual=Strings(row["_user_datas"]);var expected=Strings(reference["user_datas"]);Check(actual.Count==1&&expected.Count==1&&actual[0]==expected[0]&&actual[0].Contains("Пер-Лашез"),"Jim one translated invitation preserves prefix");
        Check(n==728&&controls==3&&pairs==2,"Jim summary728ordinary histories/3controlled entries/2actual053handoffs/47blocks137lines47answers/1call");
    }
    static void MelchiorOneChecks(Dictionary<string,object> row,Dictionary<string,object> reference) {
        var seen=new HashSet<string>();var edges=new HashSet<string>();int n=0,controls=0;
        foreach(Dictionary<string,object> r in (IEnumerable)reference["routes"]) {
            List<string> t,a;var story=ReferenceRun(row,r,(string)r["start"],null,out t,out a);AngeliqueActions(r,a);ReferenceCoverage(r,seen,edges);
            if(story.can_continue||story.GetChoices().Count!=0||a.FindAll(x=>x=="travel: start").Count!=1||a.Contains("travel: stop")||a.FindAll(x=>x=="reveal: 222_00").Count!=1||MathieuValue(story,"_Driver.Met.Melchior")!=0)throw new Exception("Melchior01 source unlinked fare/travel-stop ending remains unlinked");
            if(!t.Contains("Один глаз совсем чёрный: зрачок расширен до предела.")||!t.Contains("Другой — молочно-белый."))throw new Exception("Melchior01 pupil not burned; separate eyes");
            var path=new HashSet<string>(Strings(r["passages"]));string all=string.Join("\n",t.ToArray());
            if(path.Contains("melchior-01-intro-ok")&&(!HasText(all,"высрало")||HasText(all,"сбитая животина")))throw new Exception("Melchior01 French scatological joke, not English roadkill adaptation");
            if(path.Contains("melchior-01-intro-goodnight")&&(!HasText(all,"скоростной поезд")||HasText(all,"слонами")))throw new Exception("Melchior01 French TGV, not English elephant truck");
            if(path.Contains("melchior-01-procedure-voice")&&(!HasText(all,"запереться в ванной")||HasText(all,"запереться в спальне")))throw new Exception("Melchior01 Ade bathroom belongs to original French memory");
            if(!HasText(all,"который мог бы получить доступ")||!HasText(all,"метасети"))throw new Exception("Melchior01 hypothetical implant access not proven omniscience");
            Check(true,"Melchior01 complete finite ordinary history #"+(++n)+" exact dialogue/menus/state/actions and eye revelation");
        }
        Check(n==1728&&seen.Count==28&&edges.Count==22&&!seen.Contains("melchior-01-end"),"Melchior01 all1728ordinary histories/28blocks22answers;four late blocks unlinked in source");
        foreach(Dictionary<string,object> r in (IEnumerable)reference["controls"]) {
            List<string> t,a;var story=ReferenceRun(row,r,(string)r["start"],null,out t,out a);AngeliqueActions(r,a);ReferenceCoverage(r,seen,edges);
            Check(!story.can_continue&&story.GetChoices().Count==0&&a.FindAll(x=>x=="travel: stop").Count==1,"Melchior01 controlled original end entry #"+(++controls)+" allthree answers;not ordinary fare scene");
        }
        Check(seen.Count==32&&edges.Count==25&&controls==3,"Melchior01 all32blocks144lines25answers with explicit separate controls");
        Check(n==1728&&controls==3,"Melchior01 summary1728ordinary histories/3controlled late answers;no source link fixes");
    }
    static PrompterStory MelchiorTwoJourney(Dictionary<string,object> row,Dictionary<string,object> reference,string start,Dictionary<string,int> overrides,int firstAnswer,Dictionary<string,int> use,HashSet<string> covered,HashSet<string> edges,out List<string> path,out List<string> text,out List<string> actions) {
        var blocks=(Dictionary<string,object>)reference["blocks"];Dictionary<string,int> vars;var story=TpsStart(row,start,TpsVars(reference["initial"]),overrides,out vars,out actions);path=new List<string>();text=new List<string>();int menus=0;
        while(true) {
            var actual=Drain(story);var match=TpsMatch(story,blocks,start,vars,actual);vars=match.Vars;text.AddRange(actual);path.AddRange(match.Passages);foreach(string p in match.Passages)covered.Add(p);
            if(match.Menu=="END")break;if(++menus>40)throw new Exception("Melchior02 source menu loop");
            var choices=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)((Dictionary<string,object>)blocks[match.Menu])["choices"])choices.Add(c);
            int answer=0;
            if(menus==1&&firstAnswer>=0)answer=firstAnswer;
            else if(use!=null) {int lowest=Int32.MaxValue;for(int i=0;i<choices.Count;i++) {int used=use.ContainsKey(match.Menu+"/"+i)?use[match.Menu+"/"+i]:0;if(used<lowest){lowest=used;answer=i;}}}
            string edge=match.Menu+"/"+answer;edges.Add(edge);if(use!=null)use[edge]=(use.ContainsKey(edge)?use[edge]:0)+1;
            start=(string)choices[answer]["link"];story.ChooseAnswer(answer);
        }
        var source=new List<string>();foreach(string p in path)foreach(string command in Strings(((Dictionary<string,object>)blocks[p])["commands"]))source.Add(command.Trim().Substring(2,command.Trim().Length-4).Trim());
        AngeliqueActions(new Dictionary<string,object>{{"expected_actions",source}},actions);
        if(story.can_continue||story.GetChoices().Count!=0)throw new Exception("Melchior02 source end");return story;
    }
    static void MelchiorTwoMeaning(PrompterStory story,List<string> path,List<string> actions,List<string> text) {
        var p=new HashSet<string>(path);int apartment=MathieuValue(story,"apartment"),sidewalk=MathieuValue(story,"sidewalk"),bike=MathieuValue(story,"bike"),cash=MathieuValue(story,"cash");
        if(apartment+sidewalk+bike!=1||p.Contains("melchior-02-how-apartment")!=(apartment==1)||p.Contains("melchior-02-how-man")!=(sidewalk==1)||p.Contains("melchior-02-how-bike")!=(bike==1))throw new Exception("Melchior02 ordinary subject/event ownership");
        if(p.Contains("melchior-02-outro-cash")!=(cash==1)||p.Contains("melchior-02-exit-cash")!=(cash==1)||p.Contains("melchior-02-exit-cash-no")!=(cash==0))throw new Exception("Melchior02 cash versus account exit");
        if(apartment==1&&MathieuValue(story,"apartment-choice")!=3||sidewalk==1&&MathieuValue(story,"sidewalk_choices")!=3||bike==1&&(MathieuValue(story,"bike_choices")!=0||MathieuValue(story,"bike_var")!=111))throw new Exception("Melchior02 exact source counters including unused bike_choices");
        if(actions.FindAll(a=>a=="travel: start").Count!=1||actions.FindAll(a=>a=="travel: stop").Count!=1||actions.FindAll(a=>a.StartsWith("reveal:")).Count!=2||actions.Exists(a=>a.StartsWith("tip:")||a.StartsWith("fare:")||a.StartsWith("money:")||a.StartsWith("clue:")||a.StartsWith("destination:"))||MathieuValue(story,"_Driver.Met.Melchior")!=0||MathieuValue(story,"_GOT_KILLED")!=0)throw new Exception("Melchior02 original actions; narrated transfers not new rewards/death");
        string all=string.Join("\n",text.ToArray());
        if(p.Contains("melchior-02-apartment-do")&&(!HasText(all,"выключить телик")||!HasText(all,"Ничего особо интересного")))throw new Exception("Melchior02 original French TV joke and negative interest, not opposite English wording");
        if(p.Contains("melchior-02-apartment-work")&&(!HasText(all,"Карл пока без работы")||!HasText(all,"Ганс — архитектор ПО")))throw new Exception("Melchior02 employment owners remain separate men");
        if(p.Contains("melchior-02-sidewalk-destination")&&(!HasText(all,"порошка")||!HasText(all,"свалить быка")||HasText(all,"лошадь утопить")))throw new Exception("Melchior02 French unspecified powder/bull metaphor");
        if(p.Contains("melchior-02-end-true")&&(!HasText(all,"гомофобные выходки")||!HasText(all,"одной из депутатов")||HasText(all,"поп-звезды")))throw new Exception("Melchior02 French unnamed female politician comparison, not English popstar");
        if(!HasText(all,"кончалось пиво")||!HasText(all,"смешно шутила")||!HasText(all,"оглушительно"))throw new Exception("Melchior02 French beer/good joke/deafening flood perception");
        if(p.Contains("melchior-02-outro-saynothing")&&!HasText(all,"на операционный стол"))throw new Exception("Melchior02 French billard is repeat surgery");
        if(p.Contains("melchior-02-outro-paysilence")&&(!HasText(all,"сквозь меня")||!HasText(all,"кнопку СТОП")))throw new Exception("Melchior02 flow passes through him; wishes a STOP button");
    }
    static void MelchiorTwoChecks(Dictionary<string,object> row,Dictionary<string,object> reference) {
        var seen=new HashSet<string>();var edges=new HashSet<string>();int witnesses=0,journeys=0,controls=0;var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=50000000;string original=serializer.Serialize(row);
        foreach(Dictionary<string,object> r in (IEnumerable)reference["joint_witnesses"]) {
            List<string> t,a;var story=ReferenceRun(row,r,(string)r["start"],null,out t,out a);AngeliqueActions(r,a);ReferenceCoverage(r,seen,edges);var path=Strings(r["passages"]);MelchiorTwoMeaning(story,path,a,t);
            Check(true,"Melchior02 ordinary joint subject/event/reaction/address/payment witness #"+(++witnesses)+" exact source text/menu/state/actions");
        }
        var wanted=new HashSet<string>(Strings(reference["ordinary_passages"]));var wantedEdges=new HashSet<string>(Strings(reference["ordinary_answers"]));var use=new Dictionary<string,int>();var randomSubjects=new HashSet<string>();
        while((!seen.SetEquals(wanted)||!edges.SetEquals(wantedEdges)||randomSubjects.Count<3)&&journeys<256) {
            System.Threading.Thread.Sleep(20);List<string> path,t,a;var story=MelchiorTwoJourney(row,reference,(string)reference["start"],null,-1,use,seen,edges,out path,out t,out a);MelchiorTwoMeaning(story,path,a,t);
            if(path.Contains("melchior-02-surveillance-random"))foreach(string topic in new string[]{"apartment","sidewalk","bike"})if(path.Contains("melchior-02-surveillance-"+topic))randomSubjects.Add(topic);
            Check(true,"Melchior02 ordinary actual RNG full journey #"+(++journeys)+" exact independent source segments/menus/state/actions");
        }
        Check(witnesses==162&&seen.SetEquals(wanted)&&edges.SetEquals(wantedEdges)&&randomSubjects.Count==3,"Melchior02 ordinary actual journeys="+journeys+" cover83blocks78answers/allthree genuine random subjects plus162joint witnesses;not allCartesian histories");
        var allSeen=new HashSet<string>();var allEdges=new HashSet<string>();
        foreach(Dictionary<string,object> control in (IEnumerable)reference["controls"]) {
            string start=(string)control["start"];var overrides=TpsVars(control["overrides"]);List<string> path,t,a;MelchiorTwoJourney(row,reference,start,overrides,-1,null,allSeen,allEdges,out path,out t,out a);
            Check(path.Contains(start),"Melchior02 controlled original paragraph #"+(++controls)+" "+start);
            for(int answer=0;answer<Convert.ToInt32(control["answers"]);answer++) {MelchiorTwoJourney(row,reference,start,overrides,answer,null,allSeen,allEdges,out path,out t,out a);Check(true,"Melchior02 controlled source answer #"+(++controls)+" "+start+"/"+answer);}
        }
        foreach(Dictionary<string,object> control in (IEnumerable)reference["special"]) {
            var overrides=TpsVars(control["overrides"]);string start=(string)control["start"];List<string> path,t,a;MelchiorTwoJourney(row,reference,start,overrides,-1,null,allSeen,allEdges,out path,out t,out a);
            if(start=="melchior-02-surveillance-logic"&&path.Contains("melchior-02-surveillance-logic-2")!=(overrides["choice"]<=1)||start=="melchior-02-bike-logic"&&path.Contains("melchior-02-bike-logic-again")!=(overrides["bike_choices"]<=2))throw new Exception("Melchior02 controlled strict counter boundary");
            Check(true,"Melchior02 separate source selector/counter control #"+(++controls)+" "+start);
        }
        Check(controls==192&&allSeen.Count==96&&allEdges.Count==90,"Melchior02 all96blocks224lines90answers with192explicit controls including13unlinked fragments");
        Check(serializer.Serialize(row)==original,"Melchior02 controlled copies preserve original row variables and links");
        Check(witnesses==162&&journeys>0&&controls==192,"Melchior02 summary162ordinary joint witnesses/actualjourneys="+journeys+"/192separate controls/all96blocks224lines90answers");
    }
    static void MelchiorChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/melchior_validation_routes.json");var one=(Dictionary<string,object>)reference["one"];var two=(Dictionary<string,object>)reference["two"];
        MelchiorOneChecks((Dictionary<string,object>)rows["222_melchior_01"],one);MelchiorTwoChecks((Dictionary<string,object>)rows["222_melchior_02"],two);
        foreach(var r in new Dictionary<string,object>[]{one,two}) {var row=(Dictionary<string,object>)rows[(string)r["scene"]];var actual=Strings(row["_user_datas"]);var wanted=Strings(r["user_datas"]);Check(actual.Count==1&&wanted.Count==1&&actual[0]==wanted[0]&&actual[0].StartsWith("passengercall:"),"Melchior useful translated invitation "+r["scene"]);}
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2416.json");Check((string)card["fullname"]=="Melchior de Rochas"&&Convert.ToInt32(card["gender"])==3&&Convert.ToInt32(card["age"])==22,"Melchior male22 sourcecard and man/il; two voices not new established species or exact entity count");
    }

    static Dictionary<string,object> FactoredTextRoute(Dictionary<string,object> route,Dictionary<string,object> reference) {
        var copy=new Dictionary<string,object>(route);var units=(Dictionary<string,object>)reference["texts_by_passage"];var text=new List<string>();
        foreach(string title in Strings(route["passages"]))text.AddRange(Strings(units[title]));copy["expected_texts"]=text;return copy;
    }
    static PrompterStory BereniceRun(Dictionary<string,object> row,Dictionary<string,object> r,out List<string> text,out List<string> actions) {
        var story=ReferenceRun(row,r,(string)r["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);var p=new HashSet<string>(Strings(r["passages"]));
        if(story.can_continue||story.GetChoices().Count!=0)throw new Exception("Berenice source end");
        int starts=(p.Contains("berenice-start")?1:0)+(p.Contains("berenice-parler")?1:0),stops=(p.Contains("berenice-start")?1:0)+(p.Contains("berenice-boum-out")||p.Contains("berenice-end")?1:0);
        if(actions.FindAll(a=>a=="travel: start").Count!=starts||actions.FindAll(a=>a=="travel: stop").Count!=stops||actions.Exists(a=>a.StartsWith("money:")||a.StartsWith("fare:")||a.StartsWith("clue:")||a.StartsWith("destination:")))throw new Exception("Berenice original traffic stop/restart/end versus new police route/reward");
        var tips=actions.FindAll(a=>a.StartsWith("tip:"));var wanted=new List<string>();if(p.Contains("berenice-start"))wanted.Add("tip: low");if(p.Contains("berenice-boum-out"))wanted.Add("tip: none");
        if(string.Join("/",tips.ToArray())!=string.Join("/",wanted.ToArray()))throw new Exception("Berenice source low/none tips");
        if(p.Contains("berenice-haine-1")&&(!text.Exists(t=>t.Contains("двадцать восемь"))||!text.Exists(t=>t.Contains("четырнадцатилетней"))||!text.Contains("Вы чувствуете, как съёживаетесь на своём сиденье.")))throw new Exception("Berenice FR source28/hypothetical14/driver shrinking owner");
        if(p.Contains("berenice-complique")&&(!text.Contains("«Я не знаю».")||!text.Contains("И правда не знаете.")||!text.Contains("«Да, наверное, я уже причинял боль женщине».")||!text.Contains("«Но не так».")||!text.Contains("Пассажирка задерживает дыхание.")||!text.Contains("Она напряжённо прислушивается, ожидая ответа.")))throw new Exception("Berenice FR held breath and uncertain driver memory without invented leaning");
        if(p.Contains("berenice-haine-2")&&(!text.Contains("БЕРЕНИС: «С миллиардами женщин до меня это случалось. И после меня с миллиардами случится».")||!text.Contains("БЕРЕНИС: «Но скольким так не повезло?»")))throw new Exception("Berenice FR billions and negated rhetorical chance");
        if(p.Contains("berenice-comprendre")&&!text.Contains("БЕРЕНИС: «Больше ни в один бар не пойду»."))throw new Exception("Berenice FR any bar, not this bar only");
        if(p.Contains("berenice-boum-out")&&(!text.Contains("Может, ощущение, что вы оказались бесполезны…")||!text.Contains("Или чувство невольной ответственности.")))throw new Exception("Berenice FR alternatives are driver feelings, not established culpability");
        if(p.Contains("berenice-police1")&&(!text.Contains("Она подаётся к вам.")||!text.Contains("БЕРЕНИС: «Но нет. Не сегодня».")))throw new Exception("Berenice no invented whistle or accepted police trip");
        return story;
    }
    static void BereniceChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/berenice_validation_routes.json");var row=(Dictionary<string,object>)rows["223_berenice_01"];var seen=new HashSet<string>();var edges=new HashSet<string>();int n=0,controlled=0;
        foreach(Dictionary<string,object> raw in (IEnumerable)reference["routes"]) {
            var r=FactoredTextRoute(raw,reference);List<string> t,a;var story=BereniceRun(row,r,out t,out a);ReferenceCoverage(r,seen,edges);
            Check(MathieuValue(story,"agacement")<=2&&!Strings(r["passages"]).Contains("berenice-haine-1"),"Berenice complete finite ordinary history #"+(++n)+" exact lines/menus/state/actions;perspective not author blame");
        }
        Check(n==433&&seen.Count==25&&edges.Count==19,"Berenice all433ordinary histories cover25blocks19answers;haine1source-unreachable");
        var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=50000000;string original=serializer.Serialize(row);
        foreach(Dictionary<string,object> raw in (IEnumerable)reference["controls"]) {
            var r=FactoredTextRoute(raw,reference);var globals=(Dictionary<string,object>)r["globals"];int before=Convert.ToInt32(globals["agacement"]);var copy=PrivateVariables(row,globals);List<string> t,a;BereniceRun(copy,r,out t,out a);ReferenceCoverage(r,seen,edges);var p=new HashSet<string>(Strings(r["passages"]));
            if((string)r["start"]=="berenice-boum-0"&&p.Contains("berenice-boum")!=(before<=1)||(string)r["start"]=="berenice-haine-0"&&p.Contains("berenice-haine-1")!=(before+1>2))throw new Exception("Berenice original strict early1/2 and late2/3 threshold");
            Check(true,"Berenice separate original-ending control #"+(++controlled)+" entry="+r["start"]+" initial="+before+";not ordinary path");
        }
        Check(controlled==59&&seen.Count==26&&edges.Count==19&&serializer.Serialize(row)==original,"Berenice all26blocks208lines19answers with59explicit controls;original links/variables untouched");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2417.json");Check((string)card["fullname"]=="Bérénice Fabre"&&Convert.ToInt32(card["gender"])==0&&Convert.ToInt32(card["age"])==31,"Berenice female31card versus spoken28/around30appearance;adult not actualchild");
        var actual=Strings(row["_user_datas"]);var wanted=Strings(reference["user_datas"]);Check(actual.Count==1&&wanted.Count==1&&actual[0]==wanted[0]&&actual[0].StartsWith("passengercall : ")&&actual[0].Contains("площадь Фет"),"Berenice translated invitation retains space-before-colon prefix variant");
        Check(n==433&&controlled==59,"Berenice summary433ordinary histories/59separate boundary-ending controls/26blocks208lines19answers/1call");
    }
    static PrompterStory NadiaYvesRun(Dictionary<string,object> row,Dictionary<string,object> r,out List<string> text,out List<string> actions) {
        var story=ReferenceRun(row,r,(string)r["start"],MathieuGlobals(r["globals"]),out text,out actions);AngeliqueActions(r,actions);var p=new HashSet<string>(Strings(r["passages"]));bool good=p.Contains("ny-01-end-good"),bad=p.Contains("ny-01-end-bad"),bin=p.Contains("ny-01-end-bad-bin");int amount=MathieuValue(story,"like")>2?20:100;
        if(story.can_continue||story.GetChoices().Count!=0||good==bad||p.Contains("ny-01-end-money-low")!=(amount==20))throw new Exception("NadiaYves source end/amount condition");
        var money=new List<int>();foreach(string a in actions)if(a.StartsWith("money:"))money.Add(Int32.Parse(a.Substring(6).Trim()));
        if(money.Count!=(good?0:bin?2:1)||bad&&money[0]!=amount||bin&&money[1]!=-amount)throw new Exception("NadiaYves exact additional bill/take/trash money actions");
        if(actions.FindAll(a=>a=="travel: start").Count!=(p.Contains("ny-01-drive")?1:0)||actions.FindAll(a=>a=="travel: stop").Count!=1||actions.Exists(a=>a.StartsWith("fare:")||a.StartsWith("clue:")||a.StartsWith("destination:")))throw new Exception("NadiaYves original trip versus narrated hotel/fare");
        var tips=actions.FindAll(a=>a.StartsWith("tip:"));var wanted=new List<string>();if(p.Contains("ny-01-intro"))wanted.Add("tip: high");if(p.Contains("ny-01-end-money-honesty"))wanted.Add("tip: low");if(bad)wanted.Add("tip: none");
        if(string.Join("/",tips.ToArray())!=string.Join("/",wanted.ToArray()))throw new Exception("NadiaYves source high/low/none tips");
        if(good&&(!text.Contains("Мгновение спустя клиентка идёт домой, поддерживая своего спутника.")||!text.Contains("Он так и не проснулся.")||!text.Contains("Купюра возвращается в сумку.")))throw new Exception("NadiaYves woman supports sleeping man;extra bill returned not fare");
        if(p.Contains("ny-01-serious-02-beautiful")&&!text.Contains("Какой же вы дурак!"))throw new Exception("NadiaYves source driver selfcriticism after unwanted compliment");
        if(p.Contains("ny-01-end-bad-keep")&&!text.Contains("Буква H потрескивает и время от времени гаснет."))throw new Exception("NadiaYves Latin hotel H is not driver initial");
        if(bad&&!text.Contains("…"))throw new Exception("NadiaYves ellipses not invented curses");return story;
    }
    static void NadiaYvesChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/nadiayves_validation_routes.json");var row=(Dictionary<string,object>)rows["224_nadiayves_01"];var seen=new HashSet<string>();var edges=new HashSet<string>();int n=0,controlled=0;var likes=new HashSet<int>();
        foreach(Dictionary<string,object> raw in (IEnumerable)reference["routes"]) {
            var r=FactoredTextRoute(raw,reference);List<string> t,a;var story=NadiaYvesRun(row,r,out t,out a);ReferenceCoverage(r,seen,edges);likes.Add(MathieuValue(story,"like"));
            Check(MathieuValue(story,"like")<=2&&!Strings(r["passages"]).Contains("ny-01-end-money-low"),"NadiaYves complete finite ordinary history #"+(++n)+" exact lines/menus/state/actions and100eurobill");
        }
        Check(n==9728&&seen.Count==36&&edges.Count==40&&likes.SetEquals(new int[]{0,1,2}),"NadiaYves all9728ordinary histories/36blocks40answers/like0-2;three20euroblocks source-unreachable");
        var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=50000000;string original=serializer.Serialize(row);
        foreach(Dictionary<string,object> raw in (IEnumerable)reference["controls"]) {
            var r=FactoredTextRoute(raw,reference);var globals=(Dictionary<string,object>)r["globals"];List<string> t,a;NadiaYvesRun(PrivateVariables(row,globals),r,out t,out a);ReferenceCoverage(r,seen,edges);
            Check(true,"NadiaYves separate original ending control #"+(++controlled)+" like="+globals["like"]+";strict2/3 boundary and exact20/100 keep/trash");
        }
        Check(controlled==38&&seen.Count==39&&edges.Count==40&&serializer.Serialize(row)==original,"NadiaYves all39blocks150lines40answers with38separate controls;original source row unchanged");
        var card=ReferenceFile("original/PassengerObjectScript/sharedassets0.assets/2418.json");Check((string)card["fullname"]=="Nadia Boulanger & Yves Kilar"&&Convert.ToInt32(card["gender"])==4&&Convert.ToInt32(card["age"])==40,"NadiaYves mixedcard40 versus woman40-45/man20plus appearance;couple not mother/son or both40");
        var actual=Strings(row["_user_datas"]);var wanted=Strings(reference["user_datas"]);Check(actual.Count==1&&wanted.Count==1&&actual[0]==wanted[0]&&actual[0].Contains("Алезия")&&actual[0].Contains("четырнадцатом"),"NadiaYves translated invitation preserves district and source prefix");
        Check(n==9728&&controlled==38,"NadiaYves summary9728ordinary histories/38separate ending controls/39blocks150lines40answers/1call");
    }

    static void MathieuFrenchStructureChecks(Dictionary<string,object> rows) {
        var reference=ReferenceFile("localization/mathieu_fr_validation_routes.json");Dictionary<string,object> firstScene=null;
        foreach(Dictionary<string,object> scene in (IEnumerable)reference["scenes"])if((string)scene["scene"]=="067_mathieu_01")firstScene=scene;
        string[] hospital={"Pneumonia","CarAccident","Fall","Vague"};
        for(int old=0;old<4;old++)for(int answer=0;answer<4;answer++) {
            Dictionary<string,object> route=null;
            foreach(Dictionary<string,object> candidate in (IEnumerable)firstScene["routes"]) {
                var v=(Dictionary<string,object>)candidate["expected_variables"];
                string causeKey="_Mathieu_Hospital_"+hospital[old];
                if(v.ContainsKey("_Mathieu.Talked.Father") && Convert.ToInt32(v["_Mathieu.Talked.Father"])==1 && v.ContainsKey(causeKey) && Convert.ToInt32(v[causeKey])==1){route=candidate;break;}
            }
            if(route==null)throw new Exception("Missing actual completed first-ride hospital profile "+old);
            List<string> text,actions;var first=ReferenceRun((Dictionary<string,object>)rows["067_mathieu_01"],route,"mathieu-01-start",null,out text,out actions);
            var globals=new List<DialogVariable>{new DialogVariable("_Mathieu.Talked.Father",MathieuValue(first,"_Mathieu.Talked.Father")),new DialogVariable("_Mathieu.Talked.Talked.Father",MathieuValue(first,"_Mathieu.Talked.Talked.Father"))};
            foreach(string cause in hospital)globals.Add(new DialogVariable("_Mathieu_Hospital_"+cause,MathieuValue(first,"_Mathieu_Hospital_"+cause)));
            var second=new PrompterStory(Load((Dictionary<string,object>)rows["067_mathieu_02"]),false);var emitted=new List<string>();second.OnActionTriggered+=(e)=>emitted.Add(e.action);second.StartDialog(globals,"mathieu-02-start");
            string all=string.Join("\n",Drain(second).ToArray());
            bool ok=HasText(all,"мой отец умер сегодня под вечер") && HasText(all,"Наконец") && !HasText(all,"почти не разговаривали");
            second.ChooseAnswer(0);Drain(second);second.ChooseAnswer(0);Drain(second);
            var menu=second.GetChoices();ok &= menu.Count==4;
            string rib=(string)menu[2].GetType().GetField("_text",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(menu[2]);
            string silence=(string)menu[3].GetType().GetField("_text",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(menu[3]);
            ok &= rib=="«Сломанное ребро»." && silence==":silence: (Промолчать.)";
            second.ChooseAnswer(answer);all=string.Join("\n",Drain(second).ToArray());
            ok &= HasText(all,"Теперь вспомнил")== (old<3 && old==answer);
            ok &= HasText(all,"С пациенткой")== (old==0 && answer!=0);
            ok &= HasText(all,"Был уверен, что вы попали в аварию")== (old==1 && answer!=1);
            ok &= HasText(all,"Помню что-то о трещине в ребре")== (old==2 && answer!=2);
            ok &= HasText(all,"Вы ничего не сказали")== (old==3);
            second.ChooseAnswer(3);Drain(second);second.ChooseAnswer(0);Drain(second);
            ok &= !second.can_continue && second.GetChoices().Count==0;
            var tips=emitted.FindAll(a=>a.Contains("tip:"));ok &= tips.Count==2 && tips[0].Contains("none") && tips[1].Contains("low");
            ok &= emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*start")).Count==1 && emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*stop")).Count==1;
            ok &= MathieuValue(second,"_Mathieu.Talked.Father")==1 && MathieuValue(second,"_Mathieu.Talked.Talked.Father")==0;
            Check(ok,"Mathieu FR actual completed01flags enter02father, all four hospital answers retain target/memory/tips "+old+"/"+answer);
        }
        foreach(int typo in new int[]{0,1}) {
            var second=new PrompterStory(Load((Dictionary<string,object>)rows["067_mathieu_02"]),false);second.OnActionTriggered+=(e)=>{};
            second.StartDialog(new List<DialogVariable>{new DialogVariable("_Mathieu.Talked.Father",0),new DialogVariable("_Mathieu.Talked.Talked.Father",typo)},"mathieu-02-start");
            string text=string.Join("\n",Drain(second).ToArray());
            Check(HasText(text,"почти не разговаривали") && !HasText(text,"мой отец умер сегодня под вечер"),"Mathieu FR no actual first discussion stays quiet even with obsolete typo flag "+typo);
        }
    }

    static int Main(string[] args) {
        Console.OutputEncoding=Encoding.UTF8;
        try {
            var ser=new JavaScriptSerializer();ser.MaxJsonLength=50000000;
            var rows=ser.Deserialize<Dictionary<string,object>>(File.ReadAllText(args[0],Encoding.UTF8));
            if(args.Length>1 && args[1]=="--contextual-second-only") {ContextualSecondChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--french-navigation-only") {FrenchNavigationChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--mathieu-fr-source-only") {MathieuFrenchStructureChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--ludivine-only") {LudivineRoutes((Dictionary<string,object>)rows["054_ludivine_01"]);return 0;}
            if(args.Length>1 && args[1]=="--annabelle-only") {AnnabelleChecks((Dictionary<string,object>)rows["057_annabelle_01"]);AnnabelleSharedFrequency((Dictionary<string,object>)rows["046_shohreh_01"],(Dictionary<string,object>)rows["057_annabelle_01"]);return 0;}
            if(args.Length>1 && args[1]=="--agnes-only") {AgnesChecks((Dictionary<string,object>)rows["058_agnes_01"],(Dictionary<string,object>)rows["018_francois_00a"]);return 0;}
            if(args.Length>1 && args[1]=="--claudia-only") {ClaudiaChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--mireille-only") {MireilleChecks((Dictionary<string,object>)rows["060_mireille_01"]);return 0;}
            if(args.Length>1 && args[1]=="--denis-pauline-only") {DenisChecks((Dictionary<string,object>)rows["061_denis_01"]);PaulineChecks((Dictionary<string,object>)rows["063_pauline_01"]);return 0;}
            if(args.Length>1 && args[1]=="--janet-only") {JanetChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--nina-only") {NinaChecks((Dictionary<string,object>)rows["065_nina_01"]);return 0;}
            if(args.Length>1 && args[1]=="--tps-only") {TpsChecks((Dictionary<string,object>)rows["066_tps_01"]);return 0;}
            if(args.Length>1 && args[1]=="--mathieu-only") {MathieuChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--sonny-antoine-only") {SonnyAntoineChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--marc-pierrette-only") {MarcPierretteChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--myriam-only") {MyriamChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--chiara-only") {ChiaraChecks(rows);AdaChiaraLinkChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--leonie-only") {LeonieChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--anita-only") {AnitaChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--geraldine-only") {GeraldineChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--emilia-only") {EmiliaChecks((Dictionary<string,object>)rows["074_emilia_01"]);return 0;}
            if(args.Length>1 && args[1]=="--henri-only") {HenriChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--angelique-only") {AngeliqueChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--santa-only") {SantaChecks((Dictionary<string,object>)rows["077_santa_01"]);return 0;}
            if(args.Length>1 && args[1]=="--houssine-only") {HoussineChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--gilda-only") {GildaChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--pauline-only") {PaulineChecks((Dictionary<string,object>)rows["063_pauline_01"]);return 0;}
            if(args.Length>1 && args[1]=="--denis-only") {DenisChecks((Dictionary<string,object>)rows["061_denis_01"]);return 0;}
            if(args.Length>1 && args[1]=="--kader-only") {KaderChecks((Dictionary<string,object>)rows["056_kader_01"]);return 0;}
            if(args.Length>1 && args[1]=="--julian-only") {JulianRoutes((Dictionary<string,object>)rows["055_julian_01"],(Dictionary<string,object>)rows["030_peter_01"]);return 0;}
            if(args.Length>1 && args[1]=="--childeric-only") {ChildericChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--amelie-only") {AmelieChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--shohreh-only") {ShohrehChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--vero-only") {VeroChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--chris-only") {ChrisChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--jonas-only") {JonasChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--djena-only") {DjenaChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--francine-only") {FrancineChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--shinji-only") {ShinjiChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--sean-only") {SeanChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--sylvie-only") {SylvieChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--phil-only") {PhilChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--carlo-only") {CarloChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--skaterz-only") {SkaterzChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--alexandre-only") {AlexandreChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--ada-only") {AdaChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--milo-only") {MiloChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--chantal-amira-only") {ChantalAmiraChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--lola-only") {LolaChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--myrtille-only") {MyrtilleChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--xavier-only") {XavierChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--fragonard08-only") {Fragonard08Checks(rows);return 0;}
            if(args.Length>1 && args[1]=="--berenice-only") {BereniceChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--nadiayves-only") {NadiaYvesChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--berenice-nadiayves-only") {BereniceChecks(rows);NadiaYvesChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--jim-only") {JimChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--jim-melchior-only") {JimChecks(rows);MelchiorChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--melchior-only") {MelchiorChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--camille-only") {CamilleRoutes((Dictionary<string,object>)rows["053_camille_01"],ReferenceFile("localization/camille_validation_routes.json"));return 0;}
            if(args.Length>1 && args[1]=="--alph-only") {AlphChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--nathalie-only") {NathalieChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--alph-nathalie-only") {AlphChecks(rows);NathalieChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--roksana-only") {RoksanaChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--houssine-only") {HoussineChecks(rows);return 0;}
            if(args.Length>1 && args[1]=="--tps-threshold-only") {var r=ReferenceFile("localization/tps_validation_reference.json");for(int repeat=0;repeat<20;repeat++)TpsHealthThreshold((Dictionary<string,object>)rows["066_tps_01"],(Dictionary<string,object>)r["blocks"],TpsVars(r["initial"]));return 0;}
            var ludwig=Load((Dictionary<string,object>)rows["009_ludwig_09"]);
            string[] cases={"Judge","Sandman","Angel"};string[] scents={"Пороховой дым","прокисшее вино","Тающий воск"};
            string[] clues={"Это был подарок","Война разрушает душу","Как марионеткой"};
            for(int i=0;i<cases.Length;i++) {
                var story=Flags(ludwig,"ludwig-09-start",cases[i]);var text=string.Join("\n",Drain(story).ToArray());
                Check(text.Contains(scents[i]),cases[i]+" reaches its own smell");
                Check(story.GetChoices().Count==4,cases[i]+" keeps four separate answers");
                story.ChooseAnswer(0);text=string.Join("\n",Drain(story).ToArray());
                Check(text.Contains(clues[i]),cases[i]+" reaches its own clue");
                for(int j=0;j<cases.Length;j++)if(j!=i)Check(!text.Contains(clues[j]),cases[i]+" excludes "+cases[j]+" clue");
            }
            var generic=Flags(ludwig,"ludwig-09-start","");var gt=string.Join("\n",Drain(generic).ToArray());
            Check(gt.Contains("растопленное масло"),"Generic case keeps butter smell");generic.ChooseAnswer(0);
            gt=string.Join("\n",Drain(generic).ToArray());Check(gt.Contains("Там, где я похоронен"),"Generic case keeps grave story");
            var esmeralda=Load((Dictionary<string,object>)rows["006_esmeralda_02"]);
            var es=Start(esmeralda,"esmeralda-02-no02");Drain(es);Check(es.GetChoices().Count==3,"Esmeralda keeps truth/lie/silence choices");
            // Original FR esmeralda-02-end: La mère de la jeune fille l’attend sur le trottoir.
            es.ChooseAnswer(0);var et=string.Join("\n",Drain(es).ToArray());Check(et.Contains("Мать девушки ждёт её на тротуаре"),"Truth branch reaches mother");
            es=Start(esmeralda,"esmeralda-02-no02");Drain(es);es.ChooseAnswer(1);et=string.Join("\n",Drain(es).ToArray());
            Check(et.Contains("Но мне не нужно там побывать"),"Lie branch remains distinct");
            es=Start(esmeralda,"esmeralda-02-why");Drain(es);Check(es.GetChoices().Count==2,"Traffic light keeps stop/run choices");
            // Original FR: already red on arrival, near miss, then On a failli y passer.
            es.ChooseAnswer(0);et=string.Join("\n",Drain(es).ToArray());Check(et.Contains("уже горит красный") && et.Contains("Мы чуть не отдали концы"),"Run-light branch keeps red-light violation and near-crash");
            es=Start(esmeralda,"esmeralda-02-why");Drain(es);es.ChooseAnswer(1);et=string.Join("\n",Drain(es).ToArray());
            Check(et.Contains("Вы тормозите"),"Stop-light branch remains distinct");
            var first=Load((Dictionary<string,object>)rows["012_christophe_01"]);
            var st=Start(first,"christophe-01-yes");Drain(st);st.ChooseAnswer(1);
            var ct=string.Join("\n",Drain(st).ToArray());
            // FR driverconfession: Le soir des attentats; christstory: C’est un vieux monsieur.
            Check(ct.IndexOf("В вечер терактов")>=0 && ct.IndexOf("В вечер терактов")<ct.IndexOf("Один пожилой господин"),"Christophe driver-first order remains intact");
            st=Start(first,"christophe-01-yes");Drain(st);st.ChooseAnswer(0);ct=string.Join("\n",Drain(st).ToArray());
            Check(ct.IndexOf("Один пожилой господин")>=0 && ct.IndexOf("Один пожилой господин")<ct.IndexOf("В вечер терактов"),"Christophe priest-first order remains intact");
            var third=Load((Dictionary<string,object>)rows["012_christophe_03"]);
            foreach(int veil in new int[]{0,1}) {
                var flags=new List<DialogVariable>();flags.Add(new DialogVariable("_Christophe.Story.Veil",veil));
                st=new PrompterStory(third,false);st.OnActionTriggered+=(a)=>{};st.StartDialog(flags,"chris-03-family");
                ct=string.Join("\n",Drain(st).ToArray());
                Check(ct.Contains("Как женщина в платке")== (veil==1),"Christophe veil reference follows remembered story flag "+veil);
            }
            var fourth=Load((Dictionary<string,object>)rows["012_christophe_04"]);
            st=Start(fourth,"chris-04-name");Drain(st);st.ChooseAnswer(0);ct=string.Join("\n",Drain(st).ToArray());
            // FR name-real: not the name from dispatch; driver says they know him by another name.
            Check(!ct.Contains("Это не то имя, которое мне назвали в диспетчерской") && !ct.Contains("Там меня знают под другим именем"),"Christophe fake name does not reveal driver's alias");
            st=Start(fourth,"chris-04-name");Drain(st);st.ChooseAnswer(2);ct=string.Join("\n",Drain(st).ToArray());
            Check(ct.Contains("Это не то имя, которое мне назвали в диспетчерской") && ct.Contains("Там меня знают под другим именем"),"Christophe real name reveals driver's alias");
            Check(st.GetChoices().Count==3,"Christophe real-name follow-up keeps three answers");
            var ariane=Load((Dictionary<string,object>)rows["011_ariane_01"]);
            var arianeStory=Start(ariane,"ariane-01-nomorerain");Drain(arianeStory);arianeStory.ChooseAnswer(0);ct=string.Join("\n",Drain(arianeStory).ToArray());
            Check(ct.Contains("с достаточным запасом времени"),"Ariane running lights reaches train with time to spare");
            arianeStory=Start(ariane,"ariane-01-nomorerain");Drain(arianeStory);arianeStory.ChooseAnswer(1);ct=string.Join("\n",Drain(arianeStory).ToArray());
            Check(ct.Contains("Успеете, но впритык"),"Ariane respecting lights still reaches last train");
            var leiaFirst=Load((Dictionary<string,object>)rows["013_leia_01"]);
            string[] leiaMetNames={"Ludwig","Crouky","Fragonard","Esmeralda","Christophe"};
            string[] leiaStoryNeedles={"водит меня на экскурсии","побег от хозяйки","старого полицейского","помог ей сбежать","решил уйти из церкви"};
            for(int metIndex=0;metIndex<leiaMetNames.Length;metIndex++) {
                var metFlags=new List<DialogVariable>();
                foreach(string metName in leiaMetNames)metFlags.Add(new DialogVariable("_Driver.Met."+metName,metName==leiaMetNames[metIndex]?1:0));
                var leiaStory=new PrompterStory(leiaFirst,false);leiaStory.OnActionTriggered+=(evt)=>{};
                leiaStory.StartDialog(metFlags,"leia-01-driver-yes");ct=string.Join("\n",Drain(leiaStory).ToArray());
                Check(ct.Contains(leiaStoryNeedles[metIndex]),"Leia remembers the matching passenger "+leiaMetNames[metIndex]);
                for(int otherIndex=0;otherIndex<leiaMetNames.Length;otherIndex++)if(otherIndex!=metIndex)
                    Check(!ct.Contains(leiaStoryNeedles[otherIndex]),"Leia excludes unrelated "+leiaMetNames[otherIndex]+" story for "+leiaMetNames[metIndex]);
            }
            foreach(int trustValue in new int[]{2,3}) {
                var leiaFifth=Load((Dictionary<string,object>)rows["013_leia_05"]);
                Set(leiaFifth,"_variables",new List<DialogVariable>{new DialogVariable("trust",trustValue)});
                var leiaStory=Start(leiaFifth,"leia-05-kidsC");ct=string.Join("\n",Drain(leiaStory).ToArray());
                Check(ct.Contains("отца несколько дней назад родилась ещё одна дочь")== (trustValue>2),"Leia new sister follows trust threshold "+trustValue);
            }
            foreach(int silenceValue in new int[]{0,2}) {
                var leiaEighth=Load((Dictionary<string,object>)rows["013_leia_08"]);
                Set(leiaEighth,"_variables",new List<DialogVariable>{new DialogVariable("silence",silenceValue)});
                var leiaStory=Start(leiaEighth,"leia-08-festival-silence-hub");ct=string.Join("\n",Drain(leiaStory).ToArray());
                // FR high: pas du genre bavard; low: J’aime ... ne pas répondre ... Ça met en confiance.
                Check(ct.Contains("болтуном тебя не назовёшь")== (silenceValue==2),"Leia repeated silence follows criticism threshold "+silenceValue);
                Check(ct.Contains("Мне нравится, как ты не отвечаешь на этот вопрос. Внушает доверие.")== (silenceValue==0),"Leia brief silence keeps trust response "+silenceValue);
            }
            foreach(string killerName in new string[]{"Judge","Sandman","Angel"}) {
                var fragIntro=Load((Dictionary<string,object>)rows["014_fragonard_00a"]);
                var fragStory=Flags(fragIntro,"frag-00a-start",killerName);ct=string.Join("\n",Drain(fragStory).ToArray());
                Check(ct.Contains("ваш подозреваемый")== (killerName=="Judge"),"Fragonard first introduction restricts suspect to "+killerName);
                var fragRepeat=Load((Dictionary<string,object>)rows["014_fragonard_00b"]);
                fragStory=Flags(fragRepeat,"frag-00b-start",killerName);ct=string.Join("\n",Drain(fragStory).ToArray());
                Check(ct.Contains("Ваш подозреваемый")== (killerName=="Judge"),"Fragonard repeated introduction restricts suspect to "+killerName);
            }
            var fragChristmas=Load((Dictionary<string,object>)rows["014_fragonard_04"]);
            string[] fragChristmasAnswers={"«С семьёй.»","«Как и вы.»","«Со своей девушкой.»","«С друзьями…»"};
            for(int answer=0;answer<4;answer++) {
                var fragStory=Start(fragChristmas,"frag-04-question");Drain(fragStory);fragStory.ChooseAnswer(answer);
                ct=string.Join("\n",Drain(fragStory).ToArray());
                Check(ct.Contains(fragChristmasAnswers[answer]),"Fragonard Christmas lie remains distinct "+answer);
                Check(ct.Contains("Мёртвый.") && ct.IndexOf("отравлен",StringComparison.OrdinalIgnoreCase)>=0,"Fragonard Christmas branch reaches Ethan story "+answer);
            }
            var fragDivorce=Start(fragChristmas,"frag-04-vengeance");Drain(fragDivorce);fragDivorce.ChooseAnswer(1);
            ct=string.Join("\n",Drain(fragDivorce).ToArray());
            Check(ct.Contains("ФРАГОНАР: «Нет.»") && !ct.Contains("Зря рассказал"),"Fragonard divorce answer ends the topic immediately");
            var fragTiger=Load((Dictionary<string,object>)rows["014_fragonard_03"]);
            var fragTigerStory=Start(fragTiger,"frag-03-down");Drain(fragTigerStory);fragTigerStory.ChooseAnswer(0);
            ct=string.Join("\n",Drain(fragTigerStory).ToArray());
            // FR origin: au petit jour ... cirque ... chouré leur tigre; aftermath stays separate.
            Check(ct.Contains("Всего лишь тигр") && !ct.Contains("свистнули у них тигра"),"Fragonard tiger aftermath keeps grief without origin story");
            fragTigerStory=Start(fragTiger,"frag-03-down");Drain(fragTigerStory);fragTigerStory.ChooseAnswer(1);
            ct=string.Join("\n",Drain(fragTigerStory).ToArray());
            Check(ct.Contains("свистнули у них тигра") && !ct.Contains("Всего лишь тигр"),"Fragonard tiger origin keeps theft without aftermath branch");
            foreach(int met in new int[]{0,1}) {
                var fragStory=FragonardFinal(rows,"frag-08-reveal2",met,0);ct=string.Join("\n",Drain(fragStory).ToArray());
                Check(ct.Contains("Рак. По всему телу")== (met==0),"Fragonard explicit diagnosis follows acquaintance flag "+met);
                Check(ct.Contains("возили его в больницу")== (met==1),"Fragonard known history preserves hospital implication "+met);
                fragStory=FragonardFinal(rows,"frag-08-showdown-lie2",met,0);ct=string.Join("\n",Drain(fragStory).ToArray());
                Check(ct.Contains("Я вам открылся")== (met==1),"Fragonard lying response follows acquaintance flag "+met);
            }
            var fragDeath=FragonardFinal(rows,"frag-08-showdown-lie2-end",1,0);Drain(fragDeath);fragDeath.ChooseAnswer(0);
            ct=string.Join("\n",Drain(fragDeath).ToArray());
            Check(ct.Contains("не чувствуете ног") && fragDeath.FindVariable("_GOT_KILLED").value==1,"Fragonard fatal uncertainty kills driver with original flag");
            Check(fragDeath.FindVariable("_Fragonard.Is.Dead").value==0 && fragDeath.FindVariable("_Judge.Is.Arrested").value==0,"Fragonard driver death does not imply suicide or arrest");
            fragDeath=FragonardFinal(rows,"frag-08-story-no",1,0);Drain(fragDeath);fragDeath.ChooseAnswer(1);Drain(fragDeath);
            Check(fragDeath.FindVariable("_GOT_KILLED").value==1,"Fragonard refusing detour retains separate fatal route");
            foreach(int joker in new int[]{0,1}) {
                var fragStory=FragonardFinal(rows,"frag-08-reveal",1,joker);ct=string.Join("\n",Drain(fragStory).ToArray());
                Check(ct.Contains("Знаешь") && ct.Contains("перешёл с вами на «ты»"),"Fragonard confession preserves explicit change of address "+joker);
                Check(fragStory.GetChoices().Count==(joker==1?2:3),"Fragonard mentor conversation requires police joker "+joker);
            }
            for(int assetAnswer=0;assetAnswer<4;assetAnswer++) {
                var fragStory=FragonardFinal(rows,"frag-08-asset",1,1);Drain(fragStory);fragStory.ChooseAnswer(assetAnswer);Drain(fragStory);
                if(assetAnswer==0){fragStory.ChooseAnswer(0);Drain(fragStory);}
                ct=FragonardToBench(fragStory);
                Check(fragStory.FindVariable("asset").value==(assetAnswer==0?1:0),"Fragonard only mentor answer sets saving asset "+assetAnswer);
                if(assetAnswer==0) {
                    Check(ct.Contains("встреча с лейтенантом Бюссе") && !ct.Contains("прижимает под подбородком"),"Fragonard mentor answer returns alive to taxi");
                    fragStory.ChooseAnswer(0);ct=string.Join("\n",Drain(fragStory).ToArray());
                    Check(fragStory.FindVariable("_Judge.Is.Arrested").value==1 && fragStory.FindVariable("_Fragonard.Is.Dead").value==0 && fragStory.FindVariable("_GOT_KILLED").value==0,"Fragonard mentor ending preserves live arrest and surviving driver");
                } else {
                    Check(ct.Contains("прижимает под подбородком") && !ct.Contains("встреча с лейтенантом Бюссе"),"Fragonard non-mentor answer keeps suicide ending "+assetAnswer);
                    Check(fragStory.FindVariable("_Fragonard.Is.Dead").value==1 && fragStory.FindVariable("_Judge.Is.Arrested").value==0 && fragStory.FindVariable("_GOT_KILLED").value==0,"Fragonard suicide flags stay distinct from arrest and driver death "+assetAnswer);
                }
            }
            var fragPlea=FragonardFinal(rows,"frag-08-montmartre-intro",1,0);Drain(fragPlea);fragPlea.ChooseAnswer(2);Drain(fragPlea);
            fragPlea.ChooseAnswer(0);Drain(fragPlea);
            Check(fragPlea.FindVariable("_Fragonard.Is.Dead").value==1,"Fragonard plea at bench alone does not set saving asset");
            Fragonard08Checks(rows);
            for(int sleepAnswer=0;sleepAnswer<4;sleepAnswer++)for(int drivingAnswer=0;drivingAnswer<4;drivingAnswer++) {
                var alicia=Load((Dictionary<string,object>)rows["015_alicia_03"]);
                var aliciaStory=Start(alicia,"alicia-03-poem2");Drain(aliciaStory);aliciaStory.ChooseAnswer(sleepAnswer);Drain(aliciaStory);
                aliciaStory.ChooseAnswer(drivingAnswer);ct=string.Join("\n",Drain(aliciaStory).ToArray());
                bool completed=sleepAnswer>=2 || (sleepAnswer==0 && drivingAnswer==0);
                string combination=sleepAnswer+"/"+drivingAnswer;
                Check(ct.Contains("Одиннадцать лет пишу этот стих")==completed,"Alicia poem completion follows answer combination "+combination);
                Check(ct.Contains("тысячелетнего сна")== (sleepAnswer==3),"Alicia dream verse stays in dream branch "+combination);
                Check(ct.Contains("тысячелетнего кошмара")== (sleepAnswer==2),"Alicia nightmare verse stays in nightmare branch "+combination);
                Check(ct.Contains("Для сына и дочери настало время ужина")== (sleepAnswer==0 && drivingAnswer==0),"Alicia double refusal keeps silence verse "+combination);
                if(sleepAnswer==0 && drivingAnswer==0)
                    Check(ct.Contains("Молчание — самый холодный ответ") && !ct.Contains("Люблю ваше отсутствие"),"Alicia original noclue condition is not silently replaced by clue");
            }
            foreach(int truffleKnown in new int[]{0,1}) {
                var alicia=Load((Dictionary<string,object>)rows["015_alicia_02"]);var aliciaStory=Start(alicia,"alicia-02-start");Drain(aliciaStory);
                if(truffleKnown==1){aliciaStory.ChooseAnswer(0);Drain(aliciaStory);aliciaStory.ChooseAnswer(0);}
                else aliciaStory.ChooseAnswer(1);
                Drain(aliciaStory);
                Check(aliciaStory.GetChoices()[0].GetText().Contains("Что это за трюфели")== (truffleKnown==1),"Alicia first truffle question follows knowledge flag "+truffleKnown);
                aliciaStory.ChooseAnswer(1);ct=string.Join("\n",Drain(aliciaStory).ToArray());
                Check(ct.Contains("Нашли свои трюфели")== (truffleKnown==1),"Alicia return question remembers truffles "+truffleKnown);
                Check(ct.Contains("Что это такое")== (truffleKnown==0),"Alicia ignorance remains distinct on return "+truffleKnown);
            }
            var aliciaFirst=Load((Dictionary<string,object>)rows["015_alicia_01"]);
            var aliciaAngry=Start(aliciaFirst,"alicia-angry");Drain(aliciaAngry);aliciaAngry.ChooseAnswer(1);
            // FR end-bad-window: me voilà en train de déborder d’idées; end-bad-drive stays separate.
            ct=string.Join("\n",Drain(aliciaAngry).ToArray());Check(ct.Contains("у меня идеи через край"),"Alicia lowered window reaches inspiration through anger");
            aliciaAngry=Start(aliciaFirst,"alicia-angry");Drain(aliciaAngry);aliciaAngry.ChooseAnswer(0);
            ct=string.Join("\n",Drain(aliciaAngry).ToArray());Check(!ct.Contains("у меня идеи через край") && ct.Contains("загадочный жест"),"Alicia driving away keeps separate hostile ending");
            foreach(int previousSilence in new int[]{0,1}) {
                var gerard=Load((Dictionary<string,object>)rows["016_gerard_01"]);
                var gerardStory=Start(gerard,"gerard-01-question-intro");Drain(gerardStory);gerardStory.ChooseAnswer(previousSilence);
                ct=string.Join("\n",Drain(gerardStory).ToArray());
                Check(ct.Contains("Ты ведь сидел"),"Gerard changes to familiar address at prison question "+previousSilence);
                gerardStory.ChooseAnswer(2);ct=string.Join("\n",Drain(gerardStory).ToArray());
                Check(ct.Contains("по тебе сразу видно")== (previousSilence==1),"Gerard repeated silence follows earlier refusal "+previousSilence);
            }
            foreach(int prisonReason in new int[]{0,1}) {
                var gerard=Load((Dictionary<string,object>)rows["016_gerard_01"]);
                var gerardStory=Start(gerard,"gerard-01-jailyes");Drain(gerardStory);gerardStory.ChooseAnswer(prisonReason);
                ct=string.Join("\n",Drain(gerardStory).ToArray());
                Check((ct.IndexOf("жестокий тип",StringComparison.OrdinalIgnoreCase)>=0)== (prisonReason==0),"Gerard confession keeps truth separate from invented self-defense "+prisonReason);
            }
            var grace=Load((Dictionary<string,object>)rows["017_grace_01"]);
            var graceStory=Start(grace,"grace-01-phone01");Drain(graceStory);graceStory.ChooseAnswer(0);
            ct=string.Join("\n",Drain(graceStory).ToArray());
            Check(ct.Contains("связь… обры") && ct.Contains("Спасибо… Спасибо"),"Grace first tunnel offer reaches staged disconnection and thanks");
            graceStory=Start(grace,"grace-01-phone01");Drain(graceStory);graceStory.ChooseAnswer(1);Drain(graceStory);graceStory.ChooseAnswer(0);
            ct=string.Join("\n",Drain(graceStory).ToArray());
            Check(ct.Contains("связь… обры") && ct.Contains("Спасибо… Спасибо"),"Grace second tunnel offer reaches same help without skipping exam discussion");
            graceStory=Start(grace,"grace-01-tune");Drain(graceStory);graceStory.ChooseAnswer(0);ct=string.Join("\n",Drain(graceStory).ToArray());
            Check(ct.Contains("Я танцую") && ct.Contains("Мы с подругой"),"Grace performance is dancing with female friend");
            graceStory=Start(grace,"grace-01-phone01");Drain(graceStory);graceStory.ChooseAnswer(1);Drain(graceStory);graceStory.ChooseAnswer(1);
            Drain(graceStory);graceStory.ChooseAnswer(2);Drain(graceStory);graceStory.ChooseAnswer(2);ct=string.Join("\n",Drain(graceStory).ToArray());
            Check(ct.Contains("снова звонящего телефона") && ct.Contains("громкие голоса") && !ct.Contains("От слёз точно не осталось следа"),"Grace continuous silence follows original French bad ending; old EN happy target explicitly superseded");
            var francoisFirst=Load((Dictionary<string,object>)rows["018_francois_01"]);
            var francoisStory=Start(francoisFirst,"francois-01-stillexist");Drain(francoisStory);francoisStory.ChooseAnswer(0);
            ct=string.Join("\n",Drain(francoisStory).ToArray());
            Check(ct.Contains("приложения для мобильного телефона") && !ct.Contains("самого тупого левого правительства"),"Francois mobile game suggestion keeps curious ending");
            francoisStory=Start(francoisFirst,"francois-01-stillexist");Drain(francoisStory);francoisStory.ChooseAnswer(1);
            ct=string.Join("\n",Drain(francoisStory).ToArray());
            Check(ct.Contains("самого тупого левого правительства") && !ct.Contains("приложения для мобильного телефона"),"Francois generic agreement keeps hostile ending");
            foreach(int knowledge in new int[]{0,1}) {
                var francoisSecond=Load((Dictionary<string,object>)rows["018_francois_02"]);
                francoisStory=Start(francoisSecond,"francois-02-poetry");Drain(francoisStory);francoisStory.ChooseAnswer(knowledge);
                ct=string.Join("\n",Drain(francoisStory).ToArray());
                Check(ct.Contains("Даже таксист слышал")== (knowledge==0),"Francois pretend knowledge differs from truth "+knowledge);
            }
            foreach(int priorSilence in new int[]{1,2}) {
                var francoisSecond=Load((Dictionary<string,object>)rows["018_francois_02"]);
                Set(francoisSecond,"_variables",new List<DialogVariable>{new DialogVariable("silence",priorSilence)});
                var actionList=new List<string>();francoisStory=new PrompterStory(francoisSecond,false);
                francoisStory.OnActionTriggered+=(evt)=>actionList.Add(evt.action);francoisStory.StartDialog(null,"francois-02-end-silence");Drain(francoisStory);
                Check(actionList.Exists(a=>a.Contains("018_05"))== (priorSilence==2),"Francois silence reveal follows original threshold "+priorSilence);
            }
            string[] francoisSceneNames={"018_francois_03","018_francois_04","018_francois_08"};
            string[] francoisRevealPassages={"francois-03-silence","francois-04-end-intro-silence","francois-08-issue"};
            string[] francoisRevealIds={"018_06","018_07","018_12"};int[] francoisMinimums={2,3,2};
            for(int sceneIndex=0;sceneIndex<3;sceneIndex++)foreach(int offset in new int[]{-1,0}) {
                int priorSilence=francoisMinimums[sceneIndex]+offset;
                var d=Load((Dictionary<string,object>)rows[francoisSceneNames[sceneIndex]]);
                Set(d,"_variables",new List<DialogVariable>{new DialogVariable("silence",priorSilence)});
                var actionList=new List<string>();var testedStory=new PrompterStory(d,false);
                testedStory.OnActionTriggered+=(evt)=>actionList.Add(evt.action);testedStory.StartDialog(null,francoisRevealPassages[sceneIndex]);Drain(testedStory);
                Check(actionList.Exists(a=>a.Contains(francoisRevealIds[sceneIndex]))== (offset==0),"Francois reveal threshold "+francoisRevealIds[sceneIndex]+" at prior silence "+priorSilence);
            }
            var francoisThird=Load((Dictionary<string,object>)rows["018_francois_03"]);
            francoisStory=Start(francoisThird,"francois-03-toonice");Drain(francoisStory);francoisStory.ChooseAnswer(0);Drain(francoisStory);
            francoisStory.ChooseAnswer(0);ct=string.Join("\n",Drain(francoisStory).ToArray());
            // FR disagree: je ne vous prenais pas pour un de ces gauchistes; agree: NOS arguments.
            Check(ct.Contains("не принимал вас за одного") && !ct.Contains("НАШИ аргументы"),"Francois tooth-for-tooth answer still permits later disagreement");
            francoisStory=Start(francoisThird,"francois-03-toonice");Drain(francoisStory);francoisStory.ChooseAnswer(0);Drain(francoisStory);
            francoisStory.ChooseAnswer(1);ct=string.Join("\n",Drain(francoisStory).ToArray());
            Check(ct.Contains("НАШИ аргументы") && !ct.Contains("не принимал вас за одного"),"Francois tooth-for-tooth agreement keeps its separate political ending");
            var francoisFourth=Load((Dictionary<string,object>)rows["018_francois_04"]);
            francoisStory=Start(francoisFourth,"francois-04-iprotested-won");Drain(francoisStory);francoisStory.ChooseAnswer(0);
            ct=string.Join("\n",Drain(francoisStory).ToArray());
            Check(ct.Contains("Но мы не бастовали"),"Francois protest victory can be challenged as double standard");
            foreach(int watch in new int[]{0,1}) {
                var francoisFifth=Load((Dictionary<string,object>)rows["018_francois_05"]);
                francoisStory=Start(francoisFifth,"francois-05-stophere");Drain(francoisStory);francoisStory.ChooseAnswer(watch);
                ct=string.Join("\n",Drain(francoisStory).ToArray());
                Check(ct.Contains("New York Pizza Pizza")== (watch==0),"Francois pizza destination requires watching rather than driving away "+watch);
            }
            for(int reaction=0;reaction<4;reaction++) {
                var francoisSixth=Load((Dictionary<string,object>)rows["018_francois_06"]);
                var actionList=new List<string>();francoisStory=new PrompterStory(francoisSixth,false);
                francoisStory.OnActionTriggered+=(evt)=>actionList.Add(evt.action);francoisStory.StartDialog(null,"francois-06-gohome");Drain(francoisStory);
                francoisStory.ChooseAnswer(reaction);ct=string.Join("\n",Drain(francoisStory).ToArray());
                Check(ct.Contains("Я родился во Франции")== (reaction==3),"Francois origin confrontation preserves driver's birthplace statement "+reaction);
                if(reaction!=3){francoisStory.ChooseAnswer(1);Drain(francoisStory);}
                Check(actionList.Exists(a=>a.Contains("tip") && a.Contains("high"))== (reaction==3),"Francois high tip remains in angry origin branch "+reaction);
            }
            var francoisSeventh=Load((Dictionary<string,object>)rows["018_francois_07"]);
            francoisStory=Start(francoisSeventh,"francois-07-notes");Drain(francoisStory);francoisStory.ChooseAnswer(0);Drain(francoisStory);
            francoisStory.ChooseAnswer(1);ct=string.Join("\n",Drain(francoisStory).ToArray());
            Check(ct.Contains("в этом-то и дело") && !ct.Contains("Молчание — знак согласия"),"Francois less-noise answer follows original French silent objection; old EN silence target explicitly superseded");
            for(int question=0;question<3;question++) {
                var herveFirst=Load((Dictionary<string,object>)rows["019_herve_01"]);
                var herveStory=Start(herveFirst,"herve-01-winter2");Drain(herveStory);herveStory.ChooseAnswer(question);
                ct=string.Join("\n",Drain(herveStory).ToArray());
                Check(ct.Contains("приятелю Себу")== (question!=1),"Herve age question bypasses Seb story while ending question and silence reach it "+question);
                Check(ct.Contains("она умерла")== (question==0),"Herve mother death appears only after asking how it ended "+question);
            }
            for(int watch=0;watch<2;watch++) {
                var herveSecond=Load((Dictionary<string,object>)rows["019_herve_02"]);
                var herveStory=Start(herveSecond,"herve-02-exit");Drain(herveStory);herveStory.ChooseAnswer(watch);
                ct=string.Join("\n",Drain(herveStory).ToArray());
                Check(ct.Contains("В руке у него что-то есть")== (watch==0),"Herve unnamed object is observed only after waiting "+watch);
            }
            var herveThird=Load((Dictionary<string,object>)rows["019_herve_03"]);
            var hStory=Start(herveThird,"herve-03-story2-jealous");ct=string.Join("\n",Drain(hStory).ToArray());
            Check(ct.Contains("Она счастлива, я тоже") && ct.Contains("шлюха"),"Herve denial of jealousy preserves subsequent bitter insult");
            hStory.ChooseAnswer(1);ct=string.Join("\n",Drain(hStory).ToArray());
            Check(ct.Contains("Я о другом думал"),"Herve confrontation keeps his denial rather than objective admission");
            for(int heatAnswer=0;heatAnswer<2;heatAnswer++) {
                var herveFourth=Load((Dictionary<string,object>)rows["019_herve_04"]);
                hStory=Start(herveFourth,"herve-04-sleep-start");Drain(hStory);hStory.ChooseAnswer(heatAnswer);
                ct=string.Join("\n",Drain(hStory).ToArray());
                Check(ct.Contains("Через два дня"),"Herve charity opening remains two days away "+heatAnswer);
                hStory.ChooseAnswer(0);ct=string.Join("\n",Drain(hStory).ToArray());
                Check(ct.Contains("спасибо, что прибавил тепло")== (heatAnswer==0),"Herve heater thanks follows heat flag "+heatAnswer);
                Check(ct.Contains("нормально всё") && ct.Contains("осторожно выбирается"),"Herve hospital offer keeps refusal and original departure "+heatAnswer);
            }
            var herveWorried=Load((Dictionary<string,object>)rows["019_herve_04"]);
            hStory=Start(herveWorried,"herve-04-ignore-ok");Drain(hStory);hStory.ChooseAnswer(1);ct=string.Join("\n",Drain(hStory).ToArray());
            Check(ct.Contains("Извини") && ct.Contains("Вопросы, знаешь"),"Herve concern follows original apology then question-weariness branch");
            string[] herveCases={"Judge","Sandman","Angel",""};
            string[] herveNames={"Судью","Песочный человек","Ангела смерти",""};
            // FR Sandman >1.75m, Angel no more than1.60/1.65m; keep mutually exclusive case evidence.
            string[] herveDescriptions={"Перчатки чёрные","выше метра семидесяти пяти","не выше метра шестидесяти, шестидесяти пяти",""};
            string[] herveClueIds={"clue_judge_customer_herve_01","clue_sand_customer_herve_01","clue_angel_customer_herve_01"};
            for(int which=0;which<herveCases.Length;which++) {
                var herveFifth=Load((Dictionary<string,object>)rows["019_herve_05"]);
                hStory=Flags(herveFifth,"herve-05-killer-hub",herveCases[which]);var actions=new List<string>();
                hStory.OnActionTriggered+=(evt)=>actions.Add(evt.action);ct=string.Join("\n",Drain(hStory).ToArray());
                bool correctName=true;
                for(int other=0;other<3;other++)correctName &= ct.Contains(herveNames[other])==(which==other);
                Check(correctName,"Herve witnesses only the active killer name "+herveCases[which]);
                hStory.ChooseAnswer(0);ct=string.Join("\n",Drain(hStory).ToArray());bool correctDescription=true;
                for(int other=0;other<3;other++)correctDescription &= ct.Contains(herveDescriptions[other])==(which==other);
                Check(correctDescription,"Herve height or gloves stay exclusive to active case "+herveCases[which]);
                Check(hStory.FindVariable("_Found.Clue.Today").value==(which<3?1:0),"Herve clue flag changes only for a real case "+herveCases[which]);
                bool correctClue=true;
                for(int other=0;other<3;other++){string clueId=herveClueIds[other];correctClue &= actions.Exists(a=>a.Contains(clueId))==(which==other);}
                Check(correctClue,"Herve emits only the matching clue command "+herveCases[which]);
                hStory.ChooseAnswer(0);ct=string.Join("\n",Drain(hStory).ToArray());
                Check(ct.Contains("Может, это хитрость")== (which==1),"Herve possible deception is considered only in Sandman case "+herveCases[which]);
            }
            for(int reply=2;reply<4;reply++) {
                var herveFifth=Load((Dictionary<string,object>)rows["019_herve_05"]);
                hStory=Flags(herveFifth,"herve-05-clue-outro","Judge");Drain(hStory);hStory.ChooseAnswer(reply);
                ct=string.Join("\n",Drain(hStory).ToArray());
                Check(ct.Contains("Как это? Когда?")== (reply==2),"Herve asks about driver's attack only after explicit claim rather than silence "+reply);
            }
            for(int backAnswer=0;backAnswer<3;backAnswer++) {
                var j=Load((Dictionary<string,object>)rows["020_jacquie_00a"]);
                var js=JacquieFlags(j,"jacquie-00a-start",0,0,0);Drain(js);js.ChooseAnswer(backAnswer);
                ct=string.Join("\n",Drain(js).ToArray());
                Check(ct.Contains("не верит вашей лжи")== (backAnswer==0),"Jacquie back pain lie remains distinct from refusal and grimace "+backAnswer);
                Check(js.FindVariable("_Driver.Met.Jacquie").value==1,"Jacquie first encounter sets acquaintance flag "+backAnswer);
            }
            // FR a la Defense names the Paris business district; it does not identify a ministry employee.
            string[] jacquieSources={"До вас доходит","в мэрии работает","работает в Дефансе"};
            for(int given=0;given<3;given++) {
                var j=Load((Dictionary<string,object>)rows["020_jacquie_00b"]);
                var js=JacquieFlags(j,"jacquie-00b-killer-intro",given,0,0);var emitted=new List<string>();
                js.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(js).ToArray());
                bool ownSource=true;for(int other=0;other<3;other++)ownSource &= ct.Contains(jacquieSources[other])==(given==other);
                Check(ownSource,"Jacquie clue account follows prior clue counter "+given);
                Check(js.FindVariable("_Jacquie.Gave.Clue").value==given+1 && js.FindVariable("_Found.Clue.Today").value==1,
                    "Jacquie clue account increments counter and sets today flag "+given);
                Check(js.FindVariable("_Jacquie.Clue.Dialogue").value==1 && emitted.Exists(a=>a.Contains("fare") && a.Contains("0")),
                    "Jacquie clue account activates free-ride agreement "+given);
                Check(emitted.Exists(a=>a.Contains("clue_judge_customer_rumor_01") && a.Contains("clue_angel_customer_rumor_14") && a.Contains("clue_sand_customer_rumor_10")),
                    "Jacquie emits original clue candidate list without treating it as a selected clue "+given);
            }
            foreach(int given in new int[]{2,3}) {
                var j=Load((Dictionary<string,object>)rows["020_jacquie_00b"]);
                var js=JacquieFlags(j,"jacquie-00b-start",given,0,0);ct=string.Join("\n",Drain(js).ToArray());
                Check(!ct.Contains("Есть для тебя новости") && js.FindVariable("_Found.Clue.Today").value==0,
                    "Jacquie ordinary start after two messages preserves source route to everyday talk "+given);
            }
            for(int memory=0;memory<3;memory++) {
                var j=Load((Dictionary<string,object>)rows["020_jacquie_01"]);
                var js=JacquieFlags(j,"jacquie-01-start-silence",0,0,0);Drain(js);js.ChooseAnswer(memory);
                ct=string.Join("\n",Drain(js).ToArray());
                Check(ct.Contains("лет семнадцать")== (memory!=0),"Jacquie remembered story can skip account while denial and uncertainty continue "+memory);
            }
            foreach(int heat in new int[]{0,1})foreach(int free in new int[]{0,1}) {
                var j=Load((Dictionary<string,object>)rows["020_jacquie_01"]);
                var js=JacquieFlags(j,"jacquie-01-silence",0,free,0);var emitted=new List<string>();
                js.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(js);js.ChooseAnswer(heat);ct=string.Join("\n",Drain(js).ToArray());
                Check(emitted.Exists(a=>a.Contains("tip") && a.Contains("high"))==(heat==0),"Jacquie heater controls tip separately from fare "+heat+"/"+free);
                Check((ct.Contains("Не сегодня, дорогой") && ct.Contains("У нас, помнишь, уговор"))== (free==1) && ct.Contains("Жаки платит за проезд")== (free==0),
                    "Jacquie clue agreement controls fare independently of heater "+heat+"/"+free);
                Check(js.FindVariable("_Jacquie.Clue.Dialogue").value==0,"Jacquie first ride consumes free-ride flag "+heat+"/"+free);
            }
            foreach(int firstQuestion in new int[]{0,1}) {
                var j=Load((Dictionary<string,object>)rows["020_jacquie_03"]);
                var js=JacquieFlags(j,"jacquie-03-cooking-hub00",0,0,0);Drain(js);js.ChooseAnswer(firstQuestion);
                if(firstQuestion==0){Drain(js);js.ChooseAnswer(0);}
                ct=string.Join("\n",Drain(js).ToArray());js.ChooseAnswer(0);
                if(firstQuestion==1){Drain(js);js.ChooseAnswer(0);}
                ct+="\n"+string.Join("\n",Drain(js).ToArray());
                Check(ct.Contains("твоим братом") && ct.Contains("на парижских стройках"),"Jacquie two food questions stay available in either order "+firstQuestion);
                Check(ct.Contains("Аде") && ct.Contains("Она собиралась сказать"),"Jacquie food account preserves interrupted brother reference and female Ade "+firstQuestion);
            }
            foreach(int seen in new int[]{0,1}) {
                var j=Load((Dictionary<string,object>)rows["020_jacquie_04"]);
                var js=JacquieFlags(j,"jacquie-04-story-intro",0,0,seen);ct=string.Join("\n",Drain(js).ToArray());
                Check(ct.Contains("клиент ужинал")== (seen==1) && ct.Contains("заканчивала с клиентом")== (seen==0),
                    "Jacquie remembers food story before describing police visit "+seen);
            }
            foreach(string scene in new string[]{"02","03","04"})foreach(int free in new int[]{0,1}) {
                var j=Load((Dictionary<string,object>)rows["020_jacquie_"+scene]);
                var js=JacquieFlags(j,"jacquie-"+scene+"-end-intro",0,free,0);var emitted=new List<string>();
                js.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(js);
                Check(emitted.Exists(a=>a.Contains("fare") && a.Contains("0"))==(free==1) && js.FindVariable("_Jacquie.Clue.Dialogue").value==0,
                    "Jacquie later ride respects and consumes free-ride agreement "+scene+"/"+free);
            }
            CarloChecks(rows);
            ClaudiaChecks(rows);
            string[] apollonieTips={"high","medium","low","low","low"};int[] apollonieDestinations={0,1,3,2,-1};
            // FR Belvedere: the hypothetical remaining hearts would stop, not beat wildly.
            string[] apollonieEndTexts={"без очереди","Будет пир","последние три столетия","останемся на обочине","они бы перестали биться"};
            for(int rumors=0;rumors<8;rumors++)for(int place=0;place<5;place++)for(int fear=0;fear<4;fear++) {
                if(place<3 && (rumors&(1<<place))==0)continue;
                var d=Load((Dictionary<string,object>)rows["023_apollonie_01"]);var ap=ApollonieFlags(d,rumors);var emitted=new List<string>();
                ap.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(ap);ap.ChooseAnswer(0);Drain(ap);
                for(int candidate=0;candidate<4;candidate++) {
                    if(candidate<3 && (rumors&(1<<candidate))==0)continue;
                    ap.ChooseAnswer(candidate==place?0:1);Drain(ap);if(candidate==place)break;
                }
                bool coherent=ap.GetChoices().Count==3;ap.ChooseAnswer(0);ct=string.Join("\n",Drain(ap).ToArray());
                coherent &= ct.Contains("Пенсия") && ct.Contains("мать была рабыней");
                ap.ChooseAnswer(fear);ct=string.Join("\n",Drain(ap).ToArray());
                for(int other=0;other<5;other++) {
                    string reveal="023_0"+(other+1);coherent &= emitted.Exists(a=>a.Contains(reveal))==(place==other);
                    coherent &= ct.Contains(apollonieEndTexts[other])==(place==other);
                }
                for(int destination=0;destination<4;destination++) {
                    string pattern=@"destination\s*:\s*"+destination+@"\b";
                    coherent &= emitted.Exists(a=>Regex.IsMatch(a,pattern))==(apollonieDestinations[place]==destination);
                }
                coherent &= emitted.Exists(a=>a.Contains("tip") && a.Contains(apollonieTips[place]));
                coherent &= ap.FindVariable("_Apollonie.Met.Belvedere").value==(place==4?1:0);
                coherent &= ap.FindVariable("_Apollonie.Met.SameDay").value==1 && ap.FindVariable("_Driver.Met.Apollonie").value==1;
                coherent &= ct.Contains("От страха мясо становится жёстким")== (fear==2);
                coherent &= ct.Contains("дважды мне отказал")== (fear==0);
                coherent &= ct.Contains("кто я, дорогой")== (fear==1);
                coherent &= ct.Contains("Раньше ты был разговорчивее")== (fear==3);
                Check(coherent,"Apollonie rumor availability, destination, distinct ending, fear reply and flags "+rumors+"/"+place+"/"+fear);
            }
            foreach(int question in new int[]{0,1,2}) {
                var d=Load((Dictionary<string,object>)rows["023_apollonie_01"]);var ap=Start(d,"apollonie-drive");Drain(ap);ap.ChooseAnswer(question);
                ct=string.Join("\n",Drain(ap).ToArray());
                Check((ct.IndexOf("пенсия",StringComparison.OrdinalIgnoreCase)>=0)== (question!=2) && ct.Contains("мать была рабыней")== (question!=2),
                    "Apollonie two driver questions reach retirement story while silence skips it "+question);
            }
            var apollonieRefusal=Load((Dictionary<string,object>)rows["023_apollonie_01"]);
            var ar=ApollonieFlags(apollonieRefusal,7);var refusalActions=new List<string>();ar.OnActionTriggered+=(evt)=>refusalActions.Add(evt.action);
            Drain(ar);ar.ChooseAnswer(1);ct=string.Join("\n",Drain(ar).ToArray());
            Check(ct.Contains("ты ко мне присоединился") && ct.Contains("не годятся в вампиры") && !refusalActions.Exists(a=>a.Contains("destination")),
                "Apollonie refusal preserves joining offer and default route despite known bars");
            ar.ChooseAnswer(2);Drain(ar);ar.ChooseAnswer(3);ct=string.Join("\n",Drain(ar).ToArray());
            Check(ct.Contains("Мари-Ант") && ar.FindVariable("_Apollonie.Met.Belvedere").value==1,
                "Apollonie refusal ultimately reaches Belvedere with dedicated flag");
            foreach(int play in new int[]{0,1})foreach(int finalReply in new int[]{0,1}) {
                var d=Load((Dictionary<string,object>)rows["024_alicehyoga_01"]);var ah=Start(d,"alicehyoga-ambassador");var emitted=new List<string>();
                ah.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(ah);ah.ChooseAnswer(play==0?0:2);ct=string.Join("\n",Drain(ah).ToArray());
                bool coherent=ct.Contains("Меня укусил старый волк в лесу")== (play==1);
                ah.ChooseAnswer(1);Drain(ah);ah.ChooseAnswer(finalReply);ct=string.Join("\n",Drain(ah).ToArray());
                coherent &= ct.Contains("Мальчик в восторге")== (finalReply==0);
                coherent &= emitted.Exists(a=>a.Contains("tip") && a.Contains("none"));
                Check(coherent,"Alice intervention or play stops early with no-tip command and optional last howl "+play+"/"+finalReply);
            }
            for(int childAnswer=0;childAnswer<3;childAnswer++)foreach(int complaint in new int[]{0,1}) {
                var d=Load((Dictionary<string,object>)rows["024_alicehyoga_01"]);var ah=Start(d,"alicehyoga-ambassador");Drain(ah);ah.ChooseAnswer(1);Drain(ah);
                ah.ChooseAnswer(childAnswer);ct=string.Join("\n",Drain(ah).ToArray());ah.ChooseAnswer(complaint);ct+="\n"+string.Join("\n",Drain(ah).ToArray());
                Check(ct.Contains(childAnswer==0?"И вот результат":(childAnswer==1?"кого вы себя принимаете":"так я и думала")) &&
                    ct.Contains("не пожалуюсь вашему работодателю")== (complaint==0),
                    "Alice three childhood replies and renewed objection retain distinct contempt "+childAnswer+"/"+complaint);
            }
            for(int farewell=0;farewell<3;farewell++)foreach(int howl in new int[]{0,1}) {
                var d=Load((Dictionary<string,object>)rows["024_alicehyoga_01"]);var ah=Start(d,"alicehyoga-start");var emitted=new List<string>();
                ah.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(ah);ah.ChooseAnswer(3);ct=string.Join("\n",Drain(ah).ToArray());
                bool coherent=ct.Contains("господин Хёга") && ct.Contains("Забирает сдачу");
                ah.ChooseAnswer(farewell);ct=string.Join("\n",Drain(ah).ToArray());coherent &= ct.Contains("я знаю, что я не оборотень")== (farewell!=2);
                coherent &= ct.Contains("злая ВЕДЬМА")== (farewell!=2);
                ah.ChooseAnswer(howl);ct=string.Join("\n",Drain(ah).ToArray());coherent &= ct.Contains("Мальчик в восторге")== (howl==0);
                coherent &= !emitted.Exists(a=>a.Contains("tip") && a.Contains("none"));
                Check(coherent,"Alice uninterrupted trip and Hyoga admission depend on farewell while final howl stays optional "+farewell+"/"+howl);
            }
            for(int opening=0;opening<3;opening++)for(int ending=0;ending<2;ending++) {
                var d=Load((Dictionary<string,object>)rows["025_ade_01"]);var ad=Start(d,"ade-01-start");Drain(ad);ad.ChooseAnswer(opening);
                ct=string.Join("\n",Drain(ad).ToArray());bool coherent=ct.Contains("О ком именно ты говоришь")== (opening!=1);
                // Original FR la Justice, unlike English Judgment: do not confuse the two tarot cards.
                coherent &= ad.FindVariable("name").value==1 && ct.Contains("Верховная жрица. Дьявол. Башня") && ct.Contains("последнюю карту: Справедливость");
                ad.ChooseAnswer(ending);ct=string.Join("\n",Drain(ad).ToArray());
                coherent &= ct.Contains("сахарной пудрой") && ct.Contains("Рожки газели") && ct.Contains("Значит, правда");
                Check(coherent,"Ade politician identity question occurs once and both reactions retain pastry gift "+opening+"/"+ending);
            }
            for(int understands=0;understands<2;understands++)for(int familyReply=0;familyReply<3;familyReply++) {
                var d=Load((Dictionary<string,object>)rows["025_ade_02"]);var ad=Start(d,"ade-02-lie");var emitted=new List<string>();
                ad.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(ad);ad.ChooseAnswer(understands);ct=string.Join("\n",Drain(ad).ToArray());
                bool coherent=ct.Contains("Женщину ждало повышение")== (understands==1) && ct.Contains("начальница")== (understands==1);
                ad.ChooseAnswer(familyReply);ct=string.Join("\n",Drain(ad).ToArray());
                coherent &= ct.Contains("Твой брат был ублюдком") && emitted.Exists(a=>a.Contains("025_04"))==(familyReply==2);
                Check(coherent,"Ade admitted lie has optional work explanation and family silence has own reveal "+understands+"/"+familyReply);
            }
            for(int network=0;network<3;network++)for(int girl=0;girl<2;girl++)for(int last=0;last<3;last++) {
                var d=Load((Dictionary<string,object>)rows["025_ade_03"]);var ad=Start(d,"ade-03-clients");Drain(ad);ad.ChooseAnswer(network);
                ct=string.Join("\n",Drain(ad).ToArray());ad.ChooseAnswer(girl);ct+="\n"+string.Join("\n",Drain(ad).ToArray());
                bool coherent=ct.Contains("у начальника спроси")== (network==1) && ct.Contains("женщины — трофеи")== (girl==0);
                ad.ChooseAnswer(last);ct+="\n"+string.Join("\n",Drain(ad).ToArray());
                coherent &= ct.Contains("она лучше всех подходит для управления компанией") && ct.Contains("Вот мой сын") && ct.Contains("знаю предел его возможностей");
                Check(coherent,"Ade business story preserves client's daughter versus own son and optional commentary "+network+"/"+girl+"/"+last);
            }
            for(int route=0;route<8;route++)for(int tears=0;tears<2;tears++)for(int farewell=0;farewell<3;farewell++) {
                bool heard=route<5;if(!heard && tears==1)continue;
                var d=Load((Dictionary<string,object>)rows["025_ade_04"]);var ad=Start(d,"ade-04-start");var emitted=new List<string>();
                ad.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(ad);
                if(route==3)ad.ChooseAnswer(1);
                else if(route==4 || route==7){ad.ChooseAnswer(2);Drain(ad);ad.ChooseAnswer(route==4?1:0);}
                else {
                    ad.ChooseAnswer(0);Drain(ad);int answer=route==0?0:((route==1 || route==5)?1:2);ad.ChooseAnswer(answer);
                    if(route!=0){Drain(ad);ad.ChooseAnswer(route<5?1:0);}
                }
                ct=string.Join("\n",Drain(ad).ToArray());bool coherent=ct.Contains("Тело в Гвиане нашли")==heard;
                coherent &= ct.Contains("Префект как-то замешан")==heard;
                if(heard){ad.ChooseAnswer(tears);Drain(ad);}
                coherent &= ad.FindVariable("silence").value==(heard?0:1);
                coherent &= emitted.Exists(a=>a.Contains("025_07"))==(heard && tears==0);
                ad.ChooseAnswer(farewell);ct=string.Join("\n",Drain(ad).ToArray());
                coherent &= ct.Contains("кладёт руку вам на плечо")== (heard && farewell==0);
                coherent &= ct.Contains("И не только я")== (!heard && farewell==0);
                coherent &= ct.Contains("Со мной не надо")== (!heard && farewell==2);
                coherent &= ct.Contains("Несмотря ни на что")== (heard && farewell==2);
                coherent &= ct.Contains("кожу рулевого колеса");
                Check(coherent,"Ade eight advice routes retain listening flag, tear reveal and four farewell reactions "+route+"/"+tears+"/"+farewell);
            }
            string[] adeCases={"Sandman","Judge","Angel",""};
            string[] adeReadings={"вытянула Справедливость и Отшельника","Суд не выпал","Сразу после — Повешенный"};
            string[] adeCluePrefixes={"clue_sand_customer_ade_","clue_judge_customer_ade_","clue_angel_customer_ade_"};
            string[] adeReveals={"025_08","025_10","025_09"};
            for(int which=0;which<4;which++)for(int opening=0;opening<4;opening++) {
                var d=Load((Dictionary<string,object>)rows["025_ade_07"]);var ad=Flags(d,"ade-07-start",adeCases[which]);var emitted=new List<string>();
                ad.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(ad);ad.ChooseAnswer(opening);
                if(opening==0){Drain(ad);ad.ChooseAnswer(1);}
                else if(opening==1 || opening==3){Drain(ad);ad.ChooseAnswer(1);}
                ct=string.Join("\n",Drain(ad).ToArray());bool coherent=ad.GetChoices().Count==4;
                for(int other=0;other<3;other++) {
                    string prefix=adeCluePrefixes[other],reveal=adeReveals[other];
                    coherent &= ct.Contains(adeReadings[other])==(which==other);
                    coherent &= emitted.Exists(a=>a.Contains(prefix+"01"))==(which==other);
                    coherent &= emitted.Exists(a=>a.Contains(prefix+"02"))==(which==other);
                    coherent &= emitted.Exists(a=>a.Contains(reveal))==(which==other);
                }
                coherent &= ad.FindVariable("_Found.Clue.Today").value==(which<3?1:0);
                ad.ChooseAnswer(opening);ct=string.Join("\n",Drain(ad).ToArray());
                coherent &= ct.Contains("Ко мне и мальчикам") && ad.GetChoices().Count==3;
                Check(coherent,"Ade four entry routes preserve case-specific tarot/clues and common family invitation "+which+"/"+opening);
            }
            foreach(int opening in new int[]{0,1,3})for(int forgotten=0;forgotten<3;forgotten++)for(int dinner=0;dinner<3;dinner++) {
                var d=Load((Dictionary<string,object>)rows["025_ade_07"]);var ad=Flags(d,"ade-07-start","Judge");var emitted=new List<string>();
                ad.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(ad);ad.ChooseAnswer(opening);
                if(opening==0){Drain(ad);ad.ChooseAnswer(0);}
                Drain(ad);bool coherent=ad.GetChoices()[0].GetText()=="«Да, уверен.»";
                ad.ChooseAnswer(0);Drain(ad);ad.ChooseAnswer(forgotten);ct=string.Join("\n",Drain(ad).ToArray());
                coherent &= ct.Contains("Сумку Кевина")== (forgotten==1);
                ad.ChooseAnswer(dinner);ct=string.Join("\n",Drain(ad).ToArray());
                coherent &= ad.FindVariable("angry").value==(dinner==2?1:0);
                coherent &= ct.Contains("Это ваш дядя")== (dinner==2);
                coherent &= ct.Contains("с тех пор, как он…")== (dinner==2);
                coherent &= ct.Contains("Мог бы сегодня зайти")== (dinner!=2);
                coherent &= !emitted.Exists(a=>a.Contains("clue_")) && ad.FindVariable("_Found.Clue.Today").value==0;
                Check(coherent,"Ade final refusal has no clue, conditional Kevin name and dinner-dependent ending "+opening+"/"+forgotten+"/"+dinner);
            }
            SkaterzChecks(rows);
            for(int listen=0;listen<3;listen++)for(int genetics=0;genetics<3;genetics++)for(int response=0;response<3;response++) {
                var d=Load((Dictionary<string,object>)rows["027_lucieemilie_01"]);var le=LEStart(d,"le-01-start",0,0);var emitted=new List<string>();
                le.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(le).ToArray());bool coherent=ct.Contains("Мы с женой")==false;
                coherent &= ct.Contains("Только донора") && ct.Contains("Не отца") && ct.Contains("для твоего бывшего мужа");
                le.ChooseAnswer(listen);ct=string.Join("\n",Drain(le).ToArray());coherent &= ct.Contains("Мы с женой");
                // FR exists AND passes to the child; still only the passenger's opinion, only on yes.
                le.ChooseAnswer(genetics);ct=string.Join("\n",Drain(le).ToArray());coherent &= ct.Contains("ген напыщенности существует И передаётся")== (genetics==0);
                le.ChooseAnswer(response);Drain(le);coherent &= le.FindVariable("_LE.Trust.Driver").value==(response==0?1:-1);
                coherent &= emitted.Exists(a=>a.Contains("tip") && a.Contains(response==0?"high":"low"));
                Check(coherent,"Lucie/Emilie first ride preserves donor role, optional listening, uncertain answer and trust "+listen+"/"+genetics+"/"+response);
            }
            for(int firstResponse=0;firstResponse<3;firstResponse++)for(int opening=0;opening<4;opening++)for(int children=0;children<3;children++)for(int arrival=0;arrival<3;arrival++) {
                var d1=Load((Dictionary<string,object>)rows["027_lucieemilie_01"]);var le=LEStart(d1,"le-01-start",0,0);
                Drain(le);le.ChooseAnswer(0);Drain(le);le.ChooseAnswer(2);Drain(le);le.ChooseAnswer(firstResponse);Drain(le);
                int trust=le.FindVariable("_LE.Trust.Driver").value;
                var d2=Load((Dictionary<string,object>)rows["027_lucieemilie_02"]);le=LEStart(d2,"le-02-start",trust,0);var tips=new List<string>();
                le.OnActionTriggered+=(evt)=>{if(evt.action.Contains("tip"))tips.Add(evt.action);};Drain(le);le.ChooseAnswer(opening);Drain(le);
                le.ChooseAnswer((opening+children+arrival)%3);ct=string.Join("\n",Drain(le).ToArray());bool coherent=ct.Contains("У вас дети есть") && ct.Contains("«Нет.»");
                le.ChooseAnswer(children);Drain(le);if(children==1){le.ChooseAnswer(arrival%2);Drain(le);}
                le.ChooseAnswer(arrival);Drain(le);
                trust+=(opening==1?-1:0)+(children==0?1:0)+(arrival==1?-1:(arrival==2?-2:0))+1;
                coherent &= le.FindVariable("_LE.Trust.Driver").value==trust;
                coherent &= tips.Count==(arrival==0?1:2) && tips[tips.Count-1].Contains("high");
                if(arrival!=0)coherent &= tips[0].Contains(arrival==1?"medium":"low");
                var d3=Load((Dictionary<string,object>)rows["027_lucieemilie_03"]);le=LEStart(d3,"le-03-start",trust,0);
                Drain(le);int thirdQuestion=arrival==0?(firstResponse+opening+children)%2:2;le.ChooseAnswer(thirdQuestion);Drain(le);
                trust+=thirdQuestion<2?1:0;
                coherent &= le.FindVariable("_LE.Trust.Driver").value==trust;
                coherent &= (le.GetChoices()[0].GetText()=="«Э…»")== (trust>3);
                Check(coherent,"Lucie/Emilie full three-ride chain from zero trust preserves accumulated trust and offer gate "+firstResponse+"/"+opening+"/"+children+"/"+arrival);
            }
            foreach(int trust in new int[]{2,3,4})for(int ask=0;ask<3;ask++) {
                var d=Load((Dictionary<string,object>)rows["027_lucieemilie_03"]);var le=LEStart(d,"le-03-start",trust,0);Drain(le);le.ChooseAnswer(ask);Drain(le);
                int result=trust+(ask<2?1:0);
                Check((le.GetChoices()[0].GetText()=="«Э…»")== (result>3),"Lucie/Emilie donor proposal uses strict greater-than-three threshold "+trust+"/"+ask);
            }
            for(int interrupt=0;interrupt<4;interrupt++)for(int answer=0;answer<4;answer++) {
                var d=Load((Dictionary<string,object>)rows["027_lucieemilie_03"]);var le=LEStart(d,"le-03-pick-driver",4,0);var emitted=new List<string>();
                le.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(le);
                for(int step=0;step<3;step++){le.ChooseAnswer(step==interrupt?0:1);Drain(le);if(step==interrupt)break;}
                le.ChooseAnswer(answer);ct=string.Join("\n",Drain(le).ToArray());
                bool coherent=ct.Contains("отсюда сваливаю")== (answer==0) && ct.Contains("Люблю её")== (answer==1);
                coherent &= ct.Contains("Я вас даже не знаю")== (answer==2);
                coherent &= ct.Contains("это не… да")== (answer==3);
                coherent &= le.FindVariable("_Driver.Heard.Deal").value==(answer==3?1:0);
                coherent &= emitted.Exists(a=>a.Contains("027_03"))==(answer==3);
                coherent &= (ct.Contains("плотную визитку") && ct.Contains("номером телефона на обороте"))== (answer==3);
                if(answer==1 || answer==2){le.ChooseAnswer(interrupt%3);Drain(le);}
                le.ChooseAnswer(interrupt%2);Drain(le);
                Check(coherent,"Lucie/Emilie three interruption points or silence retain truth, fake wife, refusal and promise to think "+interrupt+"/"+answer);
            }
            string[] leCandidates={"damien","daniel","phil"};
            string[] leCandidateLines={"Я ХОЧУ МАЛЬЧИКА","Мы-то обе этого не умеем","отца только за внешность выбрали"};
            for(int which=0;which<3;which++) {
                var d=Load((Dictionary<string,object>)rows["027_lucieemilie_03"]);var le=LEStart(d,"le-03-pick-"+leCandidates[which],0,0);
                ct=string.Join("\n",Drain(le).ToArray());Check(ct.Contains(leCandidateLines[which]) && ct.Contains("Теперь моя очередь"),
                    "Lucie/Emilie each candidate branch retains distinct reason and reversed drinking roles "+leCandidates[which]);
            }
            foreach(int heard in new int[]{0,1}) {
                var d=Load((Dictionary<string,object>)rows["027_lucieemilie_03"]);var le=LEStart(d,"le-03-pick-generic-end",0,heard);var emitted=new List<string>();
                le.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(le).ToArray());
                Check((ct.Contains("плотную визитку") && ct.Contains("номером телефона на обороте"))== (heard==1) && emitted.Exists(a=>a.Contains("tip") && a.Contains("high"))==(heard==1),
                    "Lucie/Emilie business card and high tip depend on heard-deal flag "+heard);
            }
            string[] carCases={"Judge","Sandman","Angel","None"};
            string[] carCluePrefixes={"clue_judge_customer_rumor_","clue_sand_customer_rumor_","clue_angel_customer_rumor_",""};
            for(int carCase=0;carCase<4;carCase++)for(int outAnswer=0;outAnswer<2;outAnswer++)for(int ending=0;ending<4;ending++) {
                var d=Load((Dictionary<string,object>)rows["028_carolinaj_01"]);var car=CarolinaStart(d,"carolina-start",carCase,0);
                var emitted=new List<string>();car.OnActionTriggered+=(evt)=>emitted.Add(evt.action);
                ct=string.Join("\n",Drain(car).ToArray());bool coherent=ct.Contains("пожилая женщина") && ct.Contains("Каждый что-нибудь видел");
                var carClues=emitted.FindAll(a=>a.Contains("clue:"));
                coherent &= carClues.Count==(carCase<3?1:0) && car.FindVariable("_Found.Clue.Today").value==(carCase<3?1:0);
                if(carCase<3)coherent &= Regex.Matches(carClues[0],carCluePrefixes[carCase]).Count==(carCase==2?14:10);
                car.ChooseAnswer(outAnswer);ct=string.Join("\n",Drain(car).ToArray());
                coherent &= ct.Contains("это ради вас")== (outAnswer==1);
                coherent &= emitted.Exists(a=>Regex.IsMatch(a,@"travel\s*:\s*start"))==(outAnswer==0);
                int offer=(carCase+outAnswer+ending)%3;car.ChooseAnswer(offer);ct=string.Join("\n",Drain(car).ToArray());
                coherent &= ct.Contains("оставил мне здание") && ct.Contains("настоящая жизнь") && ct.Contains("Вы понимаете, о чём я");
                coherent &= ct.Contains("был немногословный")== (offer==2);
                int follow=(carCase+ending)%3;car.ChooseAnswer(follow);ct=string.Join("\n",Drain(car).ToArray());
                coherent &= ct.Contains("не очень понятно объясняю")== (follow==1);
                coherent &= ct.Contains("не захотела делать аборт") && ct.Contains("родители-гомофобы");
                int people=(carCase+outAnswer+ending)%4;car.ChooseAnswer(people);ct=string.Join("\n",Drain(car).ToArray());
                coherent &= ct.Contains("Каждый платит столько, сколько может себе позволить")== (people==1);
                coherent &= ct.Contains("снова встанут на ноги") && ct.Contains("Я ошибаюсь");
                int agree=(carCase+outAnswer+ending)%3;car.ChooseAnswer(agree);ct=string.Join("\n",Drain(car).ToArray());
                coherent &= ct.Contains("из мести")== (agree==1);
                coherent &= ct.Contains("похлопывает себя по плечу") && ct.Contains("сам просит отвезти его именно ко мне");
                coherent &= ct.Contains("хотите ли вы мне помочь") && emitted.Exists(a=>Regex.IsMatch(a,@"travel\s*:\s*stop"));
                bool accept=ending%2==0;
                if(ending<2)car.ChooseAnswer(accept?0:2);
                else {car.ChooseAnswer(1);ct=string.Join("\n",Drain(car).ToArray());coherent &= ct.Contains("Нет. Без вопросов") && car.GetChoices().Count==2;car.ChooseAnswer(accept?0:1);}
                ct=string.Join("\n",Drain(car).ToArray());
                coherent &= car.FindVariable("_Driver.Deal.Carolina").value==(accept?1:0);
                coherent &= ct.Contains("визитка")==accept && ct.Contains("Мой источник был прав")==!accept;
                coherent &= emitted.Exists(a=>a.Contains("028_01"))==accept;
                coherent &= emitted.Exists(a=>a.Contains("tip") && a.Contains(accept?"high":"low"));
                var couple=CarolinaStart(Load((Dictionary<string,object>)rows["010_camilleantoine_01"]),"ca-question",carCase,car.FindVariable("_Driver.Deal.Carolina").value);
                Drain(couple);coherent &= couple.GetChoices().Count==(accept?4:3);
                if(accept) {couple.ChooseAnswer(0);ct=string.Join("\n",Drain(couple).ToArray());coherent &= ct.Contains("отелю Каролины");}
                Check(coherent,"Carolina complete route retains case rumors, asymmetric travel, payment policy, safe referral and persistent deal "+carCases[carCase]+"/"+outAnswer+"/"+ending);
            }
            for(int observation=0;observation<3;observation++)for(int familyQuestion=0;familyQuestion<4;familyQuestion++)for(int firstExit=0;firstExit<3;firstExit++) {
                var jer=StartWithJeromeFlag(Load((Dictionary<string,object>)rows["029_jerome_01"]),"jerome-01-start");
                var emitted=new List<string>();jer.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(jer);
                jer.ChooseAnswer(observation);ct=string.Join("\n",Drain(jer).ToArray());
                bool coherent=ct.Contains("правильно расслышал")== (observation==0);
                coherent &= ct.Contains("У вас рука кровоточит")== (observation==1);
                coherent &= ct.Contains("Обычная царапина")== (observation==2);
                jer.ChooseAnswer(familyQuestion);ct=string.Join("\n",Drain(jer).ToArray());
                bool asksFather=familyQuestion==0 || familyQuestion==2;
                coherent &= ct.Contains("Он в тюрьме")==asksFather;
                if(asksFather){jer.ChooseAnswer((observation+firstExit)%2);ct=string.Join("\n",Drain(jer).ToArray());}
                coherent &= ct.Contains("купил казино в Пномпене")==asksFather;
                coherent &= ct.Contains("Я потерял работу после")== (asksFather && (observation+firstExit)%2==0);
                coherent &= ct.Contains("Они были в мусоре");
                int manhole=(observation+familyQuestion+firstExit)%3;jer.ChooseAnswer(manhole);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("крышка переворачивается")== (manhole!=2);
                int badLuck=(observation+familyQuestion+firstExit)%2;jer.ChooseAnswer(badLuck);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("невеста уходит")== (badLuck==0);
                jer.ChooseAnswer(firstExit);ct=string.Join("\n",Drain(jer).ToArray());
                bool returns=firstExit!=0;
                if(returns) {
                    coherent &= ct.Contains("Сто двенадцать") && ct.Contains("двести двенадцать");
                    int returnQuestion=(observation+familyQuestion+firstExit)%3;jer.ChooseAnswer(returnQuestion);ct=string.Join("\n",Drain(jer).ToArray());
                    coherent &= ct.Contains("Я бухгалтер")== (returnQuestion==0);
                    coherent &= ct.Contains("У меня дискалькулия")== (returnQuestion==1);
                    if(returnQuestion==1){int followup=(observation+familyQuestion)%3;jer.ChooseAnswer(followup);ct+="\n"+string.Join("\n",Drain(jer).ToArray());
                        coherent &= ct.Contains("ещё и дислексия")== (followup==0);coherent &= ct.Contains("Я бухгалтер")== (followup==1);}
                    coherent &= ct.Contains("Пожар… В доме № 212") && ct.Contains("прижимая портфель к груди");
                // FR early departure only describes a burning Haussmann building, without identifying212.
                } else coherent &= ct.Contains("Пылает большой дом в стиле Османа") && !ct.Contains("Пожар… В доме № 212");
                coherent &= jer.FindVariable("_Jerome.Burning.InCar").value==(returns?1:0);
                var jerTips=emitted.FindAll(a=>a.Contains("tip"));
                coherent &= jerTips.Count==(returns?2:1) && jerTips[0].Contains("mid");
                if(returns)coherent &= jerTips[1].Contains("low");
                int starts=emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*start")).Count;
                int expectedStarts=firstExit==0?2:(((observation+familyQuestion+firstExit)%3==1)?2:1);
                coherent &= starts==expectedStarts;
                Check(coherent,"Jerome first ride retains blood responses, optional family history, exit memory and original tip/travel sequence "+observation+"/"+familyQuestion+"/"+firstExit);
            }
            for(int burning=0;burning<2;burning++)for(int cutoff=0;cutoff<7;cutoff++)for(int waitAnswer=0;waitAnswer<2;waitAnswer++) {
                var jer=StartWithJeromeFlag(Load((Dictionary<string,object>)rows["029_jerome_02"]),"jerome-02-start",burning);
                var emitted=new List<string>();jer.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(jer).ToArray());
                bool coherent=!ct.Contains("перед горящим домом");
                jer.ChooseAnswer((cutoff+burning)%2);ct=string.Join("\n",Drain(jer).ToArray());
                var medText=new StringBuilder(ct);
                if(cutoff==0){jer.ChooseAnswer(2);ct=string.Join("\n",Drain(jer).ToArray());}
                else {
                    jer.ChooseAnswer((cutoff+burning+waitAnswer)%2);ct=string.Join("\n",Drain(jer).ToArray());medText.Append(ct);
                    if(cutoff==1){jer.ChooseAnswer(0);ct=string.Join("\n",Drain(jer).ToArray());}
                    else {
                        jer.ChooseAnswer(1+waitAnswer);ct=string.Join("\n",Drain(jer).ToArray());medText.Append(ct);
                        if(cutoff==2){jer.ChooseAnswer(0);ct=string.Join("\n",Drain(jer).ToArray());}
                        else {
                            jer.ChooseAnswer(1);ct=string.Join("\n",Drain(jer).ToArray());medText.Append(ct);
                            if(cutoff==3){jer.ChooseAnswer(0);ct=string.Join("\n",Drain(jer).ToArray());}
                            else {
                                jer.ChooseAnswer(1);ct=string.Join("\n",Drain(jer).ToArray());medText.Append(ct);
                                if(cutoff==4){jer.ChooseAnswer(0);ct=string.Join("\n",Drain(jer).ToArray());}
                                else {jer.ChooseAnswer(1);ct=string.Join("\n",Drain(jer).ToArray());medText.Append(ct);
                                    jer.ChooseAnswer(cutoff==5?0:1);ct=string.Join("\n",Drain(jer).ToArray());}
                            }
                        }
                    }
                }
                coherent &= medText.ToString().Contains("чрезмерного онанизма")== (cutoff>=3);
                coherent &= medText.ToString().Contains("в палату входит медбрат")== (cutoff>=5);
                // FR tourner de l'OEIL keeps the eye pun in the fainting expression.
                coherent &= medText.ToString().Contains("У меня чуть не потемнело в… ГЛАЗАХ")== (cutoff>=5);
                coherent &= jer.GetChoices().Count==(burning==1?2:4);
                if(burning==1){jer.ChooseAnswer(cutoff%2);ct=string.Join("\n",Drain(jer).ToArray());coherent &= ct.Contains("Это вы про пожар")== (cutoff%2==0);}
                jer.ChooseAnswer((cutoff+burning+waitAnswer)%4);Drain(jer);
                jer.ChooseAnswer((cutoff+waitAnswer)%2);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("называют „хакингом“")== ((cutoff+waitAnswer)%2==0);
                jer.ChooseAnswer((cutoff+burning)%2);Drain(jer);jer.ChooseAnswer(waitAnswer);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= jer.FindVariable("heat").value==(waitAnswer==0?1:0) && jer.FindVariable("sleep").value==0;
                coherent &= ct.Contains("номера… чеченские")== (waitAnswer==1);
                jer.ChooseAnswer((cutoff+burning+waitAnswer)%3);Drain(jer);
                jer.ChooseAnswer((cutoff+waitAnswer)%2);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("ну и жара у вас")== (waitAnswer==0);
                jer.ChooseAnswer((cutoff+burning)%2);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("Я услышал у…")== ((cutoff+burning)%2==0);
                coherent &= ct.Contains("уханье совы") && !ct.Contains("ещё спите");
                int bruno=(cutoff+burning+waitAnswer)%4;jer.ChooseAnswer(bruno);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("мамина такса")== (bruno!=3);
                coherent &= ct.Contains("раздвижную стеклянную дверь") && ct.Contains("не ВСЕ из-за меня");
                coherent &= ct.Contains("Роланд Ратценбергер") && ct.Contains("замечаете газету") && ct.Contains("Или кто-то из предыдущих");
                coherent &= emitted.Exists(a=>a.Contains("newspaper") && a.Contains("1"));
                coherent &= emitted.Exists(a=>a.Contains("tip") && a.Contains("mid"));
                coherent &= emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*start")).Count==1;
                Check(coherent,"Jerome second ride preserves fire memory, seven anecdote exits, source eye pun, wait flags and Bruno identity "+burning+"/"+cutoff+"/"+waitAnswer);
            }
            string[] jerWaitEntries={"low","mid","high","radio","journal"};
            foreach(string entry in jerWaitEntries) {
                string passage=(entry=="radio" || entry=="journal")?"jerome-02-wait-"+entry:"jerome-02-wait-sleep-"+entry;
                var jer=StartWithJeromeFlag(Load((Dictionary<string,object>)rows["029_jerome_02"]),passage);Drain(jer);
                jer.ChooseAnswer(1);Drain(jer);jer.ChooseAnswer(0);Drain(jer);jer.ChooseAnswer(1);ct=string.Join("\n",Drain(jer).ToArray());
                bool asleep=entry=="mid" || entry=="high";
                Check(ct.Contains("ещё спите")==asleep && jer.FindVariable("sleep").value==(asleep?1:0),
                    "Jerome separately entered sleep/radio/newspaper blocks preserve original dream response "+entry);
            }
            var jerFireIntro=StartWithJeromeFlag(Load((Dictionary<string,object>)rows["029_jerome_02"]),"jerome-02-startB",1);
            ct=string.Join("\n",Drain(jerFireIntro).ToArray());
            Check(ct.Contains("перед горящим домом") && jerFireIntro.GetChoices().Count==2,"Jerome disconnected fire introduction remains translated and separately playable");
            for(int jr3Family=0;jr3Family<7;jr3Family++)for(int jr3Warning=0;jr3Warning<5;jr3Warning++)for(int jr3Wave=0;jr3Wave<2;jr3Wave++) {
                var jer=Start(Load((Dictionary<string,object>)rows["029_jerome_03"]),"jerome-03-start");
                var emitted=new List<string>();jer.OnActionTriggered+=(evt)=>emitted.Add(evt.action);ct=string.Join("\n",Drain(jer).ToArray());
                bool coherent=ct.Contains("той… совы в Сент-Уэне") && ct.Contains("назначили финансовым директором");
                jer.ChooseAnswer((jr3Family+jr3Warning)%3);Drain(jer);
                int scout=(jr3Family+jr3Warning+jr3Wave)%3;jer.ChooseAnswer(scout);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("Отмывание денег")== (scout==0);
                // FR peers went TO hospital; carcinomas have no specified basal-cell subtype.
                coherent &= ct.Contains("отправились в местную больницу")== (scout==1);
                coherent &= ct.Contains("дюжина карцином")== (scout==1);
                coherent &= emitted.Exists(a=>a.Contains("029_06"))==(scout==1);
                if(jr3Family<2){jer.ChooseAnswer(jr3Family==0?0:2);ct=string.Join("\n",Drain(jer).ToArray());}
                else {
                    jer.ChooseAnswer(1);ct=string.Join("\n",Drain(jer).ToArray());
                    coherent &= ct.Contains("единственный сын") && ct.Contains("всё ещё живёт дома");
                    if(jr3Family<4){jer.ChooseAnswer(jr3Family==2?1:2);ct=string.Join("\n",Drain(jer).ToArray());}
                    else {jer.ChooseAnswer(0);ct=string.Join("\n",Drain(jer).ToArray());coherent &= ct.Contains("Нас трое") && ct.Contains("два года назад");
                        jer.ChooseAnswer(jr3Family==4?0:(jr3Family==5?1:2));ct=string.Join("\n",Drain(jer).ToArray());}
                }
                coherent &= ct.Contains("Ей… девятнадцать")== (jr3Family==4);
                coherent &= ct.Contains("он передал тайное послание")== (jr3Family==2 || jr3Family==5);
                coherent &= ct.Contains("Антильских островах") && ct.Contains("счета — в Панаме") && ct.Contains("носить ШЛЯПУ");
                coherent &= ct.Contains("велел ничего не делать") && ct.Contains("ПАНАМСКИЕ документы");
                jer.ChooseAnswer((jr3Family+jr3Wave)%3);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("любимчик номер два") && ct.Contains("в завещании");
                for(int step=0;step<4;step++){jer.ChooseAnswer(step==jr3Warning?0:1);ct=string.Join("\n",Drain(jer).ToArray());if(step==jr3Warning)break;}
                bool warned=jr3Warning<4;bool gunKnown=jr3Warning>=2;
                coherent &= jer.FindVariable("gun").value==(gunKnown?1:0);
                coherent &= emitted.Exists(a=>a.Contains("029_07"))==warned;
                coherent &= ct.Contains("преследует предпринимателей")==warned;
                coherent &= ct.Contains("размером с игрушку")==!warned;
                if(warned) {
                    int advice=(jr3Family+jr3Wave)%3;jer.ChooseAnswer(advice);ct=string.Join("\n",Drain(jer).ToArray());
                    coherent &= ct.Contains("следует одному-единственному правилу")== (advice!=2);
                    coherent &= ct.Contains("Никаких тайных счетов у него нет")== (advice!=2);
                }
                // Original French theatre-superstition joke, not the English duck/museum/animals adaptation.
                coherent &= ct.Contains("Дерьмо, дерьмо, дерьмо") && ct.Contains("Комеди Франсез") && ct.Contains("С суевериями там НЕ ШУТЯТ");
                int jr3Farewell=(jr3Family+jr3Warning+jr3Wave)%4;jer.ChooseAnswer(jr3Farewell);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("правда желаете мне удачи")== (jr3Farewell==0);
                coherent &= ct.Contains("Хорошее предчувствие")== (jr3Farewell==1 || jr3Farewell==2);
                coherent &= ct.Contains("единственный")== (jr3Farewell==3);
                coherent &= ct.Contains("Все пять ступеней") && ct.Contains("указывает на пистолет на поясе дежурного")==gunKnown;
                jer.ChooseAnswer(jr3Wave);ct=string.Join("\n",Drain(jer).ToArray());
                coherent &= ct.Contains("Портфель падает на землю")== (jr3Wave==0) && ct.Contains("сталкиваются лбами")== (jr3Wave==0);
                coherent &= emitted.FindAll(a=>a.Contains("tip")).Count==1 && emitted.Exists(a=>a.Contains("tip") && a.Contains("mid"));
                coherent &= emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*start")).Count==1 && emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*stop")).Count==1;
                Check(coherent,"Jerome third ride retains seven family routes, four warning points or silence, hat misunderstanding and farewell callbacks "+jr3Family+"/"+jr3Warning+"/"+jr3Wave);
            }
            for(int peterMet=0;peterMet<8;peterMet++)for(int peterCase=0;peterCase<4;peterCase++)for(int wake=0;wake<2;wake++)for(int musicAnswer=0;musicAnswer<3;musicAnswer++) {
                var pe=PeterFlags(Load((Dictionary<string,object>)rows["030_peter_01"]),"Peter-01-start",peterMet,peterCase);
                var emitted=new List<string>();pe.OnActionTriggered+=(evt)=>emitted.Add(evt.action);Drain(pe);
                bool coherent=pe.FindVariable("_Driver.Met.Peter").value==1 && pe.FindVariable("_Found.Clue.Today").value==(peterCase<3?1:0);
                pe.ChooseAnswer((peterMet+peterCase+wake)%2);ct=string.Join("\n",Drain(pe).ToArray());
                bool alicia=(peterMet&1)!=0, julian=!alicia && (peterMet&2)!=0, shohreh=!alicia && !julian && (peterMet&4)!=0;
                coherent &= ct.Contains("неповторимый голос одной из своих пассажирок")==alicia;
                coherent &= ct.Contains("политическом скандале")==julian;
                coherent &= ct.Contains("Женщину-математика")==shohreh;
                if(shohreh)coherent &= ct.Contains("она только что получила");
                int expectedClues=(peterCase<3?1:0)+(peterMet==0 && peterCase<3?1:0);
                coherent &= emitted.FindAll(a=>a.Contains("clue:")).Count==expectedClues;
                if(peterMet==0) {
                    string[] radioTopics={"расследование убийств Судьи","деле Песочного человека","деле Ангела смерти","Поэтессу расспрашивают"};
                    coherent &= ct.Contains(radioTopics[peterCase]);
                }
                pe.ChooseAnswer(wake);ct=string.Join("\n",Drain(pe).ToArray());
                coherent &= ct.Contains("Я не спал, я трезвел")== (wake==0) && ct.Contains("Диджей Блант");
                pe.ChooseAnswer(musicAnswer);ct=string.Join("\n",Drain(pe).ToArray());
                // FR kid nods; observation, not formal lessons. Stade performance is present/future, not a completed past summer.
                coherent &= ct.Contains("научил этого парня всему") && ct.Contains("Он качает головой.") && ct.Contains("Нет… Но можешь смотреть") && ct.Contains("продавал дозы экстази");
                coherent &= ct.Contains("Знаешь, что он делает этим летом") && ct.Contains("Стад де Франс");
                int watch=(musicAnswer+wake)%2;pe.ChooseAnswer(watch);ct=string.Join("\n",Drain(pe).ToArray());
                coherent &= ct.Contains("Тонкие, редкие волосы")== (watch==0) && ct.Contains("женщину, идущую по пешеходному переходу")== (watch==1);
                coherent &= ct.Contains("четыре-пять назад") && ct.Contains("Диджей ёбаный мудак");
                pe.ChooseAnswer((peterMet+peterCase)%2);ct=string.Join("\n",Drain(pe).ToArray());
                coherent &= ct.Contains("телефон между передними сиденьями") && ct.Contains("драм-машина") && ct.Contains("темп не падает");
                // FR explicitly parks sur le trottoir; payment includes coins as well as crumpled banknotes.
                coherent &= ct.Contains("пассажир засыпает") && ct.Contains("Такси припарковано на тротуаре") && ct.Contains("купюры и несколько монет") && ct.Contains("Пятнадцать минут славы");
                coherent &= emitted.Exists(a=>a.Contains("030_01"));
                var peterTips=emitted.FindAll(a=>a.Contains("tip"));int expectedTips=(wake==0?1:0)+(musicAnswer==0?1:0);
                coherent &= peterTips.Count==expectedTips;
                if(wake==0)coherent &= peterTips[0].Contains("low");
                if(musicAnswer==0)coherent &= peterTips[peterTips.Count-1].Contains("high");
                coherent &= emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*start")).Count==1 && emitted.FindAll(a=>Regex.IsMatch(a,@"travel\s*:\s*stop")).Count==1;
                Check(coherent,"Peter complete route retains radio identity priority, Shohreh gender, tip order and phone playback "+peterMet+"/"+peterCase+"/"+wake+"/"+musicAnswer);
            }
            foreach(string station in new string[]{"A","B"}) {
                var pe=PeterFlags(Load((Dictionary<string,object>)rows["030_peter_01"]),"Peter-01-radio01"+station,0,3);ct=string.Join("\n",Drain(pe).ToArray());
                Check(ct.Contains("Музыка рай")== (station=="A") && ct.Contains("Григорианские песнопения")== (station=="B"),
                    "Peter both separately entered random station alternatives remain translated "+station);
            }
            GeraldineChecks(rows);
            AnitaChecks(rows);
            ChiaraChecks(rows);
            PhilChecks(rows);
            SylvieChecks(rows);
            SeanChecks(rows);
            ShinjiChecks(rows);
            FrancineChecks(rows);
            DjenaChecks(rows);
            JonasChecks(rows);
            ChrisChecks(rows);
            VeroChecks(rows);
            ShohrehChecks(rows);
            AmelieChecks(rows);
            LeonieChecks(rows);
            ChildericChecks(rows);
            CamilleRoutes((Dictionary<string,object>)rows["053_camille_01"],ser.Deserialize<Dictionary<string,object>>(File.ReadAllText("localization/camille_validation_routes.json",Encoding.UTF8)));
            LudivineRoutes((Dictionary<string,object>)rows["054_ludivine_01"]);
            JulianRoutes((Dictionary<string,object>)rows["055_julian_01"],(Dictionary<string,object>)rows["030_peter_01"]);
            KaderChecks((Dictionary<string,object>)rows["056_kader_01"]);
            AnnabelleChecks((Dictionary<string,object>)rows["057_annabelle_01"]);
            AnnabelleSharedFrequency((Dictionary<string,object>)rows["046_shohreh_01"],(Dictionary<string,object>)rows["057_annabelle_01"]);
            AgnesChecks((Dictionary<string,object>)rows["058_agnes_01"],(Dictionary<string,object>)rows["018_francois_00a"]);
            MireilleChecks((Dictionary<string,object>)rows["060_mireille_01"]);
            ContextualSecondChecks(rows);
            FrenchNavigationChecks(rows);
            DenisChecks((Dictionary<string,object>)rows["061_denis_01"]);
            PaulineChecks((Dictionary<string,object>)rows["063_pauline_01"]);
            JanetChecks(rows);
            NinaChecks((Dictionary<string,object>)rows["065_nina_01"]);
            TpsChecks((Dictionary<string,object>)rows["066_tps_01"]);
            MathieuChecks(rows);
            SonnyAntoineChecks(rows);
            MarcPierretteChecks(rows);
            MyriamChecks(rows);
            EmiliaChecks((Dictionary<string,object>)rows["074_emilia_01"]);
            HenriChecks(rows);
            AngeliqueChecks(rows);
            SantaChecks((Dictionary<string,object>)rows["077_santa_01"]);
            HoussineChecks(rows);
            GildaChecks(rows);
            AlexandreChecks(rows);
            AdaChecks(rows);
            MiloChecks(rows);
            ChantalAmiraChecks(rows);
            LolaChecks(rows);
            MyrtilleChecks(rows);
            XavierChecks(rows);
            RoksanaChecks(rows);
            AlphChecks(rows);
            NathalieChecks(rows);
            JimChecks(rows);
            MelchiorChecks(rows);
            BereniceChecks(rows);
            NadiaYvesChecks(rows);
            return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
