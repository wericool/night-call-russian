using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using Prompter.Runtime;

class Fragonard04SourceChecks {
    const BindingFlags Hidden=BindingFlags.Static|BindingFlags.NonPublic;
    static object Call(string name,params object[] args) {return typeof(FullContextStoryChecks).GetMethod(name,Hidden).Invoke(null,args);}
    static List<string> Strings(object data) {var r=new List<string>();foreach(object x in (IEnumerable)data)r.Add((string)x);return r;}
    static void Check(bool value,string message) {if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
    static int Main(string[] args) {
        try {
            var js=new JavaScriptSerializer();js.MaxJsonLength=int.MaxValue;
            var rows=js.Deserialize<Dictionary<string,object>>(File.ReadAllText(args[0]));
            var reference=js.Deserialize<Dictionary<string,object>>(File.ReadAllText(args[1]));
            var row=(Dictionary<string,object>)rows["014_fragonard_04"];
            var blocks=(Dictionary<string,object>)reference["blocks"];
            int blockCount=0,answerCount=0;
            foreach(Dictionary<string,object> p in (IEnumerable)row["_passages"]) {
                var b=(Dictionary<string,object>)blocks[(string)p["_title"]];blockCount++;
                Check((string)p["_link"]==(string)b["link"] && Strings(p["_variable_modifiers"]).Count==0,"Fragonard04 original French link/empty modifiers "+p["_title"]);
                var visible=new List<string>();var commands=new List<string>();
                foreach(string line in Strings(p["_lines"]))if(line.TrimStart().StartsWith("$$"))commands.Add(line);else visible.Add(line);
                Check(js.Serialize(visible)==js.Serialize(Strings(b["texts"])) && js.Serialize(commands)==js.Serialize(Strings(b["commands"])),"Fragonard04 exact manual text and original commands "+p["_title"]);
                var choices=new List<Dictionary<string,object>>();foreach(Dictionary<string,object> c in (IEnumerable)p["_choices"])choices.Add(c);
                int i=0;foreach(Dictionary<string,object> c in (IEnumerable)b["choices"]) {
                    Check(i<choices.Count && (string)choices[i]["_text"]==(string)c["text"] && (string)choices[i]["_link"]==(string)c["link"] && Convert.ToBoolean(choices[i]["_ending"])==Convert.ToBoolean(c["ending"]),"Fragonard04 original answer/marker/target "+p["_title"]+"/"+i);i++;answerCount++;
                }
                Check(i==choices.Count,"Fragonard04 exact original menu size "+p["_title"]);
            }
            Check(blockCount==27 && answerCount==14 && js.Serialize(Strings(row["_user_datas"]))==js.Serialize(Strings(reference["user_datas"])) && Strings(row["_user_datas"]).Count==8,"Fragonard04 27blocks/14answers/8original definitions and no new calls");
            int variables=0;foreach(object variable in (IEnumerable)row["_variables"])variables++;
            Check(variables==0,"Fragonard04 original empty variable declaration retained");
            int count=0;var reached=new HashSet<string>();var covered=new HashSet<string>();
            foreach(Dictionary<string,object> route in (IEnumerable)reference["routes"]) {
                var invoke=new object[]{row,route,"frag-04-start",null,null,null,true};
                var story=(PrompterStory)Call("ReferenceRun",invoke);
                var text=(List<string>)invoke[4];var actions=(List<string>)invoke[5];
                Call("AngeliqueActions",route,actions);
                foreach(string p in Strings(route["passages"]))reached.Add(p);
                foreach(object edge in (IEnumerable)route["edges"]) {var e=new List<object>();foreach(object x in (IEnumerable)edge)e.Add(x);covered.Add(e[0]+"/"+e[1]);}
                string whole=string.Join("\n",text.ToArray());var passages=Strings(route["passages"]);
                bool divorce=passages.Contains("frag-04-judge-divorce");
                bool ok=!story.can_continue && story.GetChoices().Count==0;
                ok &= whole.Contains("Этан") && whole.Contains("двенадцать") && whole.Contains("двадцать два");
                ok &= whole.Contains("Зря я об этом заговорил")==!divorce;
                ok &= actions.FindAll(a=>a.Contains("reveal: 014_08")).Count==1;
                ok &= actions.FindAll(a=>a.Contains("reveal: 014_07")).Count==(passages.Contains("frag-04-vengeance")?1:0);
                Check(ok,"Fragonard04 actual complete original history "+(++count)+" exact text/menu/action sequence; distinct lies and divorce ending");
            }
            Check(count==96 && reached.Count==27 && covered.Count==14,"Fragonard04 all96 original complete histories cover27blocks/14answer edges; no live Unity claim");
            return 0;
        } catch(Exception e) {Console.Error.WriteLine(e);return 1;}
    }
}
