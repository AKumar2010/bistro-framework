﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bistro.Controllers.Descriptor;

namespace Bistro.Engine.Methods.Generation
{


    internal class GenBinding
    {
        internal GenBinding(string _url, string _controller, bool _matchStatus,BindVerb _verb)
        {
            initialUrl = _url;
            initialController = _controller;
            matchStatus = _matchStatus;

            items = GetSplitItems(initialUrl);

        }


        #region Private members
        private static Regex splitRegex = new Regex(@"/\?/|\?/|/\?", RegexOptions.Compiled);
        private static Regex subSplitRegex = new Regex(@"/", RegexOptions.Compiled);
        private static Regex wildCardRegex = new Regex(@"\A(?:\*|{[^}]+})\z", RegexOptions.Compiled);


        private List<List<string>> items;
        private string initialUrl;

        private BindVerb verb;



        private string initialController;

        private bool matchStatus;

        #endregion

        #region private methods

        private List<List<string>> GetSplitItems(string splitUrl)
        {
            var preSplitItems = splitRegex.Split(splitUrl);
            var tempItems = new List<List<string>>(preSplitItems.Length);
            foreach (string preSplitItem in preSplitItems)
            {
                var splitItems = subSplitRegex.Split(preSplitItem).Where(inputStr => inputStr != string.Empty);
                tempItems.Add(new List<string>(splitItems));
            }
            return tempItems;
        }


        #endregion




        #region Internal properties
        internal BindVerb Verb
        {
            get { return verb; }
            set { verb = value; }
        }
        internal string InitialUrl
        {
            get { return initialUrl; }
            set { initialUrl = value; }
        }

        internal bool MatchStatus
        {
            get { return matchStatus; }
        }

        #endregion

        #region Internal methods
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodUrlsSubset"></param>
        /// <returns></returns>
        /// 
        internal bool MatchWithSubset(MethodUrlsSubset methodUrlsSubset)
        {
            List<GenBinding> newList = new List<GenBinding>(methodUrlsSubset.BindingsList);
            IEnumerable<GenBinding> matchBindings = newList.Where(binding => binding.MatchStatus);
            IEnumerable<GenBinding> noMatchBindings = newList.Where(binding => !binding.MatchStatus);

            List<string> firstPartOfThis = this.items.Count == 0 ? new List<string>() : new List<string>(this.items[0]);

            
            if (this.MatchStatus)
            {


                foreach (GenBinding binding in matchBindings)
                {
                    List<string> firstPart = binding.items.Count == 0 ? new List<string>() : new List<string>(binding.items[0]);
                    #region Check for different beginning

                    int smallestSize = firstPartOfThis.Count < firstPart.Count ? firstPartOfThis.Count : firstPart.Count;


                    for (int i = 0; i < smallestSize; i++)
                    {
                        if ((wildCardRegex.IsMatch(firstPart[i])) || (wildCardRegex.IsMatch(firstPartOfThis[i])) || (firstPartOfThis[i] == firstPart[i]))
                            continue;
                        return false;
                    }

                    #endregion
                }






                foreach (GenBinding binding in noMatchBindings)
                {
                    List<string> firstPart = binding.items.Count == 0 ? new List<string>() : new List<string>(binding.items[0]);


                    #region Get the start point firstPartOfThis - A; firstPart - B
                    if (firstPart.Count > firstPartOfThis.Count)
                        continue;

                    bool firstItemMatchImpossible = false;
                    for (int i = 0; i < firstPart.Count; i++)
                    {
                        if ((wildCardRegex.IsMatch(firstPart[i])) || (firstPartOfThis[i] == firstPart[i]))
                            continue;

                        firstItemMatchImpossible = true;
                        break;
                    }

                    if (firstItemMatchImpossible)
                        continue;

                    var currentPartEnum = binding.items.GetEnumerator();
                    currentPartEnum.MoveNext();
                    var currentMatchPartEnum = this.items.GetEnumerator();
                    //                    currentMatchPartEnum.MoveNext();

                    int positionInMatchPart = (currentPartEnum.Current == null) ? 0 : currentPartEnum.Current.Count;

                    List<string> currentPart = (currentPartEnum.MoveNext()) ? currentPartEnum.Current : null;
                    List<string> currentMatchPart = (currentMatchPartEnum.MoveNext()) ? currentMatchPartEnum.Current : null;



                    #endregion

                    while ((currentPart != null) && (currentMatchPart != null))
                    {
                        //try to place it.
                        if (positionInMatchPart + currentPart.Count <= currentMatchPart.Count)
                        {
                            bool placed = true;
                            for (int i = 0; i < currentPart.Count; i++)
                            {
                                if (wildCardRegex.IsMatch(currentPart[i]))
                                    continue;
                                if (currentPart[i] == currentMatchPart[i + positionInMatchPart])
                                    continue;

                                placed = false;
                                break;
                            }

                            if (placed)
                            {
                                positionInMatchPart = positionInMatchPart + currentPart.Count;
                                currentPartEnum.MoveNext();
                                currentPart = currentPartEnum.Current;
                            }
                            else
                                positionInMatchPart++;

                        }
                        else
                        {
                            currentMatchPartEnum.MoveNext();
                            currentMatchPart = currentMatchPartEnum.Current;
                            positionInMatchPart = 0;
                        }

                    }

                    if ((currentPart == null) && (currentMatchPart != null))
                    {
                        // noMatch binding completely matches with one of the match bindings.
                        return false;
                    }


                }



            }
            else
            {
                foreach (GenBinding binding in matchBindings)
                {
                    List<string> firstPart = binding.items.Count == 0 ? new List<string>() : new List<string>(binding.items[0]);


                    #region Get the start point firstPartOfThis - B; firstPart - A
                    if (firstPartOfThis.Count > firstPart.Count)
                        continue;

                    bool firstItemMatchImpossible = false;
                    for (int i = 0; i < firstPartOfThis.Count; i++)
                    {
                        if ((wildCardRegex.IsMatch(firstPartOfThis[i])) || (firstPart[i] == firstPartOfThis[i]))
                            continue;

                        firstItemMatchImpossible = true;
                        break;
                    }

                    if (firstItemMatchImpossible)
                        continue;

                    var currentPartEnum = this.items.GetEnumerator();
                    currentPartEnum.MoveNext();
                    var currentMatchPartEnum = binding.items.GetEnumerator();
                    //                    currentMatchPartEnum.MoveNext();

                    int positionInMatchPart = (currentPartEnum.Current == null) ? 0 : currentPartEnum.Current.Count;

                    List<string> currentPart = (currentPartEnum.MoveNext()) ? currentPartEnum.Current : null;
                    List<string> currentMatchPart = (currentMatchPartEnum.MoveNext()) ? currentMatchPartEnum.Current : null;



                    #endregion

                    while ((currentPart != null) && (currentMatchPart != null))
                    {
                        //try to place it.
                        if (positionInMatchPart + currentPart.Count <= currentMatchPart.Count)
                        {
                            bool placed = true;
                            for (int i = 0; i < currentPart.Count; i++)
                            {
                                if (wildCardRegex.IsMatch(currentPart[i]))
                                    continue;
                                if (currentPart[i] == currentMatchPart[i + positionInMatchPart])
                                    continue;

                                placed = false;
                                break;
                            }

                            if (placed)
                            {
                                positionInMatchPart = positionInMatchPart + currentPart.Count;
                                currentPartEnum.MoveNext();
                                currentPart = currentPartEnum.Current;
                            }
                            else
                                positionInMatchPart++;

                        }
                        else
                        {
                            currentMatchPartEnum.MoveNext();
                            currentMatchPart = currentMatchPartEnum.Current;
                            positionInMatchPart = 0;
                        }

                    }

                    if ((currentPart == null) && (currentMatchPart != null))
                    {
                        // noMatch binding completely matches with one of the match bindings.
                        return false;
                    }


                }






            }

            return true;


        }

        #region old MatchWithSubSet implementation
//        internal bool MatchWithSubSet(MethodUrlsSubset methodUrlsSubset)
//        {
//            #region create common url with all matched bindings.

//            List<string> commonBinding = new List<string>();

//            List<GenBinding> newList = new List<GenBinding>(methodUrlsSubset.BindingsList);
//            newList.Add(this);

//            IEnumerable<GenBinding> matchBindings = newList.Where(binding => binding.MatchStatus);
//            IEnumerable<GenBinding> noMatchBindings = newList.Where(binding => !binding.MatchStatus);
//            foreach (GenBinding binding in matchBindings)
//            {
//                List<string> firstPart = binding.items.Count == 0 ? new List<string>() : new List<string>(binding.items[0]);
//                #region equalization
//                int difference = Math.Abs(commonBinding.Count - firstPart.Count);

//                if (commonBinding.Count > firstPart.Count)
//                {
//                    for (int i = 0; i < difference; i++)
//                        firstPart.Add("*");
//                }
//                else
//                {
//                    for (int i = 0; i < difference; i++)
//                        commonBinding.Add("*");
//                }

//                #endregion

//                for (int i = 0; i < commonBinding.Count; i++)
//                {
//                    if ((wildCardRegex.IsMatch(firstPart[i])) || (commonBinding[i] == firstPart[i]))
//                        continue;

//                    if (wildCardRegex.IsMatch(commonBinding[i]))
//                    {
//                        commonBinding[i] = firstPart[i];
//                        continue;
//                    }

//                    return false;
//                }


//            }
//            #endregion



//            #region exclude items based on nomatch bindings
//            foreach (GenBinding binding in noMatchBindings)
//            {
//                List<string> firstPart = binding.items.Count == 0 ? new List<string>() : new List<string>(binding.items[0]);
//                #region equalization
//                int difference = Math.Abs(commonBinding.Count - firstPart.Count);

//                if (commonBinding.Count > firstPart.Count)
//                {
//                    for (int i = 0; i < difference; i++)
//                        firstPart.Add("*");
//                }
//                else
//                {
//                    continue;///???????????
//                    for (int i = 0; i < difference; i++)
//                    {
//                        commonBinding.Add("*");
//                    }
//                }

//                #endregion

//                bool firstNoMatchImpossible = true;

//                for (int i = 0; i < commonBinding.Count; i++)
//                {
//                    if ((wildCardRegex.IsMatch(firstPart[i])) || (commonBinding[i] == firstPart[i]))
//                        continue;

//                    firstNoMatchImpossible = false;
//                    break;
//                }

//                #region if the first part of the noMatch matches the commonBinding - the rest(second,thirs, etc) must be checked for every item from the matchBindings
//                if (firstNoMatchImpossible)
//                {
//                    foreach (GenBinding matchBind in matchBindings)
//                    {

//                        var currentPartEnum = binding.items.GetEnumerator();
//                        currentPartEnum.MoveNext();
//                        var currentMatchPartEnum = matchBind.items.GetEnumerator();
////                        currentMatchPartEnum.MoveNext();

//                        int positionInMatchPart = (currentPartEnum.Current == null) ? 0 : currentPartEnum.Current.Count;

//                        List<string> currentPart = (currentPartEnum.MoveNext()) ? currentPartEnum.Current : null;
//                        List<string> currentMatchPart = (currentMatchPartEnum.MoveNext()) ? currentMatchPartEnum.Current : null;


//                        while ((currentPart != null) && (currentMatchPart != null))
//                        {
//                            //try to place it.
//                            if (positionInMatchPart + currentPart.Count <= currentMatchPart.Count)
//                            {
//                                bool placed = true;
//                                for (int i = 0; i < currentPart.Count; i++)
//                                {
//                                    if (wildCardRegex.IsMatch(currentPart[i]))
//                                        continue;
//                                    if (currentPart[i] == currentMatchPart[i + positionInMatchPart])
//                                        continue;

//                                    placed = false;
//                                    break;
//                                }

//                                if (placed)
//                                {
//                                    positionInMatchPart = positionInMatchPart + currentPart.Count;
//                                    currentPartEnum.MoveNext();
//                                    currentPart = currentPartEnum.Current;
//                                }
//                                else
//                                    positionInMatchPart++;

//                            }
//                            else
//                            {
//                                currentMatchPartEnum.MoveNext();
//                                currentMatchPart = currentMatchPartEnum.Current;
//                                positionInMatchPart = 0;
//                            }

//                        }

//                        if ((currentPart == null) && (currentMatchPart != null))
//                        {
//                            // noMatch binding completely matches with one of the match bindings.
//                            return false;
//                        }

//                    }


//                }

//                #endregion
//            }
//            #endregion


//            return true;
//        }
        #endregion


        #endregion



    }
}