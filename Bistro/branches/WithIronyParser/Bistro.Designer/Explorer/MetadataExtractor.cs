﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Irony.Parsing;
using Irony.Samples.CSharp;
using Irony.Samples.FSharp;
using Bistro.MethodsEngine.Reflection;
using Bistro.Interfaces;
using Bistro.Controllers.Descriptor;

namespace Bistro.Designer.Explorer
{
    
    using ControllersTable = Dictionary<string, Dictionary<string, List<string>>>;
    public class MetadataExtractor
    {

        /// <summary>
        ///info about controllers is stored here after parser finished parsing current file
        ///format key=ctrlName,value={key=attribute,value=parameters' values}
        /// </summary>
        public ControllersTable controllerInfo;
        /// <summary>
        /// format key = absolute src filename,value={controllerInfo(see above)}
        /// </summary>
        public Dictionary<string, ControllersTable> infobyFiles;
        public MetadataExtractor(string lang, string fname)
        {
            parseTree = null;
            if (String.Compare(lang, "c#") == 0)
                grammar = new CSharpGrammar();
            else if (String.Compare(lang, "f#") == 0)
                grammar = new FSharpGrammar();
            else
            {
                throw new Exception("The grammar name you've typed is not supported");

            }
            parser = new Parser(new LanguageData(grammar));
            srcFilename = fname;
            controllerInfo = new ControllersTable();
            infobyFiles = new Dictionary<string, ControllersTable>();
        }
        public string FileName
        {
            set { srcFilename = value; }
            get { return srcFilename; }
        }
        /// <summary>
        /// Method is used to extract controllers' data from specified source file into ControllersTable 
        /// </summary>
        /// <returns>true if essential changes in controllers' declarations were detected,
        /// else - otherwise</returns>
        public bool FillControllerInfo()
        {
            TextReader rd = new StreamReader(srcFilename);
            controllerInfo.Clear();
            Extract(rd.ReadToEnd(), "<source>");
            if (parseTree == null) return false;
            AddParseNodeRec(parseTree.Root);
            //if source file has been recently added to the project,add new row into ControllersTable
            //else if there were any changes in source file,update corresponding row in ControllersTable
            if (!infobyFiles.ContainsKey(srcFilename))
            {
                ControllersTable curtbl = new ControllersTable(controllerInfo);
                infobyFiles.Add(srcFilename, curtbl);
                return true;
            }
            else
            {
                bool equal = new AttributesComparer().Equals(infobyFiles[srcFilename], controllerInfo);
                if (!equal)
                {
                    if (infobyFiles.Remove(srcFilename))
                    {
                        ControllersTable curtbl = new ControllersTable(controllerInfo);
                        infobyFiles.Add(srcFilename, curtbl);
                        return true;
                    }
                }
                return false;
            }
        }

        #region Private members
        private ParseTree parseTree;
        private Parser parser;
        private Grammar grammar;
        private string srcFilename;
        private const string tail = " (identifier)";
        private string curCtrl;//name of the controller that is being processed
        private string curAttr;//name of the attribute that is being processed
        private int bindTargs;//as Dictionary key must be unique,so we'll save it like Bind0..BindN for each controller 
        private bool isBindAttr;//this flag shows whether curAttr is BindAttribute 
        private void Extract(string srcText, string fname)
        {
            try
            {
                parser.Parse(srcText, fname);
            }
            catch (Exception ex)
            {
            }
            finally
            {
                parseTree = parser.Context.CurrentParseTree;
            }
        }

        private void AddParseNodeRec(ParseTreeNode nodeInfo)
        {
            if (nodeInfo == null) return;
            Token token = nodeInfo.AstNode as Token;
            if (token != null)
            {
                if (token.Terminal.Category != TokenCategory.Content) return;
            }
            string txt = nodeInfo.ToString();
            if (grammar.GetType() == typeof(FSharpGrammar)&& (String.Compare(txt, "FunctionDef") == 0) )
            {
                Trace.WriteLine(txt + " of " + nodeInfo.ChildNodes[2] + " :");
                curCtrl = nodeInfo.ChildNodes[2].ToString();
                curCtrl = curCtrl.Substring(0,curCtrl.Length - tail.Length);
                controllerInfo.Add(curCtrl, new Dictionary<string, List<string>>());
                controllerInfo[curCtrl].Add("DependsOn",new List<string>());
                controllerInfo[curCtrl].Add("Provides",new List<string>());
                controllerInfo[curCtrl].Add("Requires",new List<string>());
                bindTargs = 0;
            }
            else if (grammar.GetType() == typeof(CSharpGrammar) && (String.Compare(txt, "class_declaration") == 0))
            {
                //Not implemented yet
                Trace.WriteLine(txt + " of " + nodeInfo.ChildNodes[3] + " :");
                curCtrl = nodeInfo.ChildNodes[3].ToString();
                curCtrl = curCtrl.Substring(0, curCtrl.Length - tail.Length);
                controllerInfo.Add(curCtrl, new Dictionary<string, List<string>>());
                bindTargs = 0;

            }
            foreach (var child in nodeInfo.ChildNodes)
            {

                ParseTreeNode curChild = child;
                bool isAttribute  = String.Compare(child.ToString(), "attribute") == 0;
                bool isAttrArgs = String.Compare(child.ToString(), "attr_arg") == 0;
                bool isParam = String.Compare(child.ToString(), "Param") == 0;
                if (isAttribute)
                {
                    while (curChild.ChildNodes.Count > 0)
                    {
                        curChild = curChild.ChildNodes[0];
                    }
                    Trace.WriteLine("<attr name>:" + curChild.ToString());
                    curAttr = curChild.ToString();
                    curAttr = curAttr.Substring(0, curAttr.Length - tail.Length);
                    isBindAttr = String.Compare(curAttr, "Bind") == 0;
                    if (isBindAttr)
                    {
                        curAttr += (bindTargs++).ToString() ;
                        
                    }
                    controllerInfo[curCtrl].Add(curAttr, new List<string>());
                }
                else if (isAttrArgs)
                {
                    string curVal = String.Empty;
                    bool isFullName = false;
                    //go down until we reach the leaf
                    /// TODO : refactor this branch in more simple way
                    ///
                    if (isBindAttr)
                    {
                        //Bind("target") - arg is Literal NT is processed as other common attr_args
                        //Bind("target",Priority=1,ControllerBindType = BindType.After)- args are Literal,BinExpr,BinExpr
                        while (curChild.ChildNodes.Count > 0)
                        {
                            curChild = curChild.ChildNodes[0];
                            //Processing of named parameters
                            if (String.Compare(curChild.ToString(), "BinExpr") == 0)
                            {
                                if (curChild.ChildNodes[0].ToString() == "Expr")
                                {
                                    //process Expr-Literal = Expr->Literal
                                    ParseTreeNode node = (curChild.ChildNodes[0]).ChildNodes[0];
                                    if (node.ToString() == "Literal")
                                    {
                                        curVal = node.ChildNodes[0].ToString() + "= ";//Expr -> Literal->""
                                        curChild = curChild.ChildNodes[2];//right Expr
                                        if (curChild.ChildNodes[0].ToString() == "qual_name_segments_opt")
                                        {
                                            curChild = (curChild.ChildNodes[0]).ChildNodes[node.ChildNodes.Count - 1];//Literal
                                            curVal += curChild.ChildNodes[0].ToString();
                                        }
                                        else
                                        {
                                            curVal += (curChild.ChildNodes[0]).ChildNodes[0].ToString();

                                        }
                                    }
                                    Trace.WriteLine("<val>:" + curChild.ToString());
                                    (controllerInfo[curCtrl][curAttr]).Add(curVal);
                                    break;
                                }

                            }
                            else if (String.Compare(curChild.ToString(), "Literal") == 0)
                            {
                                while (curChild.ChildNodes.Count > 0)
                                {
                                    curChild = (isFullName) ? curChild.ChildNodes[curChild.ChildNodes.Count - 1] : curChild.ChildNodes[0];
                                }
                                Trace.WriteLine("<val>:" + curChild.ToString());
                                curVal += curChild.ToString();
                                (controllerInfo[curCtrl][curAttr]).Add(curVal);

                            }
                        }
                    }
                    else
                    {
                        while (curChild.ChildNodes.Count > 0)
                        {
                            curChild = (isFullName) ? curChild.ChildNodes[curChild.ChildNodes.Count - 1] : curChild.ChildNodes[0];
                        }
                        Trace.WriteLine("<val>:" + curChild.ToString());
                        curVal += curChild.ToString();
                        (controllerInfo[curCtrl][curAttr]).Add(curVal);
                    }
                }
                else if (isParam)
                {
                    string paramName = curChild.ChildNodes[1].ToString();
                    paramName = paramName.Substring(0, paramName.Length - tail.Length);
                    /* According to the FSharpGrammar (see Irony.Samples):
                     * curChild.ChildNodes[0] = param_attr_opt NT
                     * curChild.ChildNodes[0]).ChildNodes[1] = qual_name_segment_opt NT
                     */
                    curChild = (curChild.ChildNodes[0]).ChildNodes[1];
                    //go down until we reach the leaf - Attribute(StringLiteral)
                    while (curChild.ChildNodes.Count > 0)
                    {
                        curChild = curChild.ChildNodes[curChild.ChildNodes.Count - 1];
                    }
                    string paramAttr = curChild.ToString();
                    paramAttr = paramAttr.Substring(0, paramAttr.Length - tail.Length);
                    bool needToStore = String.Compare("DependsOn",paramAttr) == 0 
                        || String.Compare("Provides",paramAttr) == 0
                        || String.Compare("Requires",paramAttr) == 0;
                    //Note : DependsOn,Provides,Requires keys are added when curCtrl is added
                    if (needToStore)
                        controllerInfo[curCtrl][paramAttr].Add(paramName);
                }

                AddParseNodeRec(child);
            }

        }
        #endregion


    }
    public class BindPointDescription : IMethodsBindPointDesc
    {
        /// <summary>
        /// BindAttribute Syntax Example [<Bind("target",Priority = 1,ControllerBindType = BindType.After,Target="target")>]
        /// </summary>
        public BindPointDescription(ControllerDescription _ctrlDesc, int num)
        {
            ctrlDesc = _ctrlDesc;
            strBind = "Bind" + num.ToString();
            Initialize();
        }
        private ControllerDescription ctrlDesc;
        private string strBind;
        private BindType bindType;
        private int bindLen;
        private int priority;
        private string target;
        private void Initialize()
        {
            if (ctrlDesc.attrs.ContainsKey(strBind))
            {
                //TODO: usually target is the first parameter,but it can be set via named parameter Target
                string[] splitTarget = ctrlDesc.attrs[strBind][0].Split(' ');
                target =  splitTarget[0];
                if ((String.Compare(target, "get") == 0 || String.Compare(target, "post") == 0) && splitTarget.Length > 1) 
                    target = splitTarget[1];
                
                foreach (string param in ctrlDesc.attrs[strBind])
                {
                    if (param.Contains("ControllerBindType"))
                    {
                        string[] type = param.Split(' ');
                        switch (type[2])
                        {
                            case "After":
                                bindType = BindType.After;
                                break;
                            case "Before":
                                bindType = BindType.Before;
                                break;
                            case "TearDown":
                                bindType = BindType.Teardown;
                                break;
                            case "PayLoad":
                                bindType = BindType.Payload;
                                break;
                        }
                    }
                    else if (param.Contains("Priority"))
                    {
                        string[] splitPrior = param.Split(' ');
                        priority = Convert.ToInt32(splitPrior[2]);
                    }

                }
            }
            else
            {
                //set default values
                bindType = BindType.After;
                priority = -1;
                target = "";
            }
            bindLen = BindPointUtilities.GetBindComponents(Target).Length;
            
        }
        #region IMethodsBindPointDesc Members

        public int BindLength
        {
            get { return bindLen; }
        }

        public IMethodsControllerDesc Controller
        {
            get { return ctrlDesc; }
        }

        public Bistro.Controllers.Descriptor.BindType ControllerBindType
        {
            ///"controllerBindType(identifier)= After (identifier)
            get { return bindType; }
        }

        public int Priority
        {
            ///"Priority (identifier)= 1 (number)
            get { return priority; }
              
        }

        public string Target
        {
            get { return target; }
        }

        #endregion
    }
    public class ControllerDescription : IMethodsControllerDesc
    {
        public ControllerDescription(string _name, Dictionary<string, List<string>> _attrs)
        {
            name = _name;
            attrs = _attrs;
            nTargs = 0;
            foreach (KeyValuePair<string, List<string>> kvp in attrs)
            {
                if (kvp.Key.Contains("Bind"))
                    nTargs++;
            }
            targs = new List<IMethodsBindPointDesc>(nTargs);

        }
        public Dictionary<string, List<string>> attrs;
        private string name;
        private int nTargs;
        private List<IMethodsBindPointDesc> targs;

        #region IMethodsControllerDesc Members

        public string ControllerTypeName
        {
            get { return name; }
        }

        public List<string> DependsOn
        {
            get 
            {
                return attrs["DependsOn"];
            }
        }

        public string GetResourceType(string resourceName)
        {
            return resourceName + "resourceType";
        }

        public bool IsSecurity
        {
            get { return attrs.ContainsKey("Security"); }
        }

        public List<string> Provides
        {
            get { return attrs["Provides"]; }  
        }

        public List<string> Requires
        {
            get { return attrs["Requires"]; }
        }

        public IEnumerable<IMethodsBindPointDesc> Targets
        {
            get 
            { 
                for (int i = 0;i < nTargs; i++)
                {
                    targs.Add(new BindPointDescription(this, i));
                }
                return targs;
            }
        }

        #endregion
    }
   
}