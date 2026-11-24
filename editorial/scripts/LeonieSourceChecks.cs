using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using Prompter.Runtime;

class LeonieSourceChecks {
    const BindingFlags Hidden=BindingFlags.Static|BindingFlags.NonPublic;
    static object Call(string name,params object[] args) {
        return typeof(FullContextStoryChecks).GetMethod(name,Hidden).Invoke(null,args);
    }
    static void Check(bool ok,string text) {if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
    static PrompterStory Start(Dictionary<string,object> row,string passage,int boring) {
        var d=(Dialog)Call("Load",row);
        foreach(DialogVariable variable in (IEnumerable)d.GetType().GetField("_variables",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(d))
            if(variable.name=="boring")variable.GetType().GetField("_value",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(variable,boring);
        return (PrompterStory)Call("LeonieStart",d,passage,3,null);
    }
    static string Drain(PrompterStory story) {return string.Join("\n",((List<string>)Call("Drain",story)).ToArray());}
    static int Main(string[] args) {
        try {
            var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=int.MaxValue;
            var rows=serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText(args[0]));
            var row=(Dictionary<string,object>)rows["051_leonie_01"];
            bool declared=false;foreach(Dictionary<string,object> v in (IEnumerable)row["_variables"])declared|=(string)v["_name"]=="music-jazz";
            Check(!declared,"Leonie source has no declared music-jazz variable; editorial input preserved it");
            var jazz=Start(row,"leonie-01-question-music-jazz",0);Drain(jazz);
            Check(jazz.FindVariable("music-jazz")!=null && jazz.FindVariable("music-jazz").value==1,"Prompter creates music-jazz=1 from the original modifier without adding a variable to resources");
            for(int value=2;value<=3;value++) {
                var story=Start(row,"leonie-01-thesis2",value);string text=Drain(story);
                Check(text.Contains("Вы не можете сдержать зевок")==(value==3) && text.Contains("Вы зеваете во второй раз")== (value==3) && story.FindVariable("boring").value==value+1,
                    "Leonie thesis2 yawn uses strict >3 after source increment, controlled initial boring="+value);
            }
            for(int value=3;value<=4;value++) {
                var story=Start(row,"leonie-01-thesis4-people",value);string text=Drain(story);
                Check(text.Contains("подавляете огромный зевок")== (value==4) && story.FindVariable("boring").value==value+1,
                    "Leonie people yawn uses strict >4 after source increment, controlled initial boring="+value);
            }
            return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
