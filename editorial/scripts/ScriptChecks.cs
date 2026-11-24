using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Prompter.Runtime;
using PrompterCompiler.Parser;
class ScriptChecks {
    static void Check(bool value,string label){if(!value)throw new Exception(label);}
    static Dialog Compile(string file){
        var method=typeof(PrompterParser).GetMethod("Compile",new Type[]{typeof(string),typeof(bool)});
        return (Dialog)method.Invoke(method.IsStatic?null:Activator.CreateInstance(typeof(PrompterParser)),new object[]{Path.GetFullPath(file),false});
    }
    static int Main(string[] args){
        try{
            var en=Compile(args[0]);var ru=Compile(args[1]);
            Check(en!=null&&ru!=null,"Compiler returned null");
            Check(en.passages.Count==ru.passages.Count,"Passage count");
            for(int i=0;i<en.passages.Count;i++){
                var a=en.passages[i];var b=ru.passages[i];
                Check(a.title==b.title,"Passage title");Check(a.GetLink()==b.GetLink(),"Passage navigation");
                Check((a.variable_modifiers??new System.Collections.Generic.List<string>()).SequenceEqual(b.variable_modifiers??new System.Collections.Generic.List<string>()),"Variable modifiers");
                var al=a.lines??new System.Collections.Generic.List<string>();var bl=b.lines??new System.Collections.Generic.List<string>();
                var ac=a.choices??new System.Collections.Generic.List<ChoiceSection>();var bc=b.choices??new System.Collections.Generic.List<ChoiceSection>();
                Check(al.Count==bl.Count,"Line coverage");
                Check(al.Where(s=>s.Trim().StartsWith("$$")).SequenceEqual(bl.Where(s=>s.Trim().StartsWith("$$"))),"Commands");
                Check(ac.Count==bc.Count,"Choice count");
                for(int k=0;k<ac.Count;k++){
                    Check(a.choices[k].GetLink()==b.choices[k].GetLink(),"Choice navigation");
                    Check(a.choices[k].IsEnding()==b.choices[k].IsEnding(),"Choice ending");
                }
            }
            Check(en.variables.Select(v=>v.ToString()).SequenceEqual(ru.variables.Select(v=>v.ToString())),"Initial variables");
            Console.WriteLine("PASS real Prompter compiler: "+en.passages.Count+" passages, "+en.GetChoiceCount()+" choices; all navigation, commands, variables and line counts preserved.");
            return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
