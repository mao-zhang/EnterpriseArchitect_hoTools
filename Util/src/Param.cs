﻿using System;
using System.Text.RegularExpressions;
using System.Xml;
using EA;

// ReSharper disable once CheckNamespace
namespace hoTools.Utils.Parameter
{
    //---------------------------------------------------------------------------------------
    // Param Allows to set properties for Activity Parameter
    //---------------------------------------------------------------------------------------
    // update properties for return value
    //            Param par = new Param(rep, parTrgt);
    //            par.setParameterProperties("direction", "out");
    //            par.save();
    //            par = null;
    public class Param
    {
        readonly Repository _rep;
        readonly EA.Element _parTrgt; // parameter
        string _xrefid = "";        // GUID of t_xref
        string _properties = "";    // the properties
        public Param(Repository rep, EA.Element parTrgt) {
            _rep = rep;
            _parTrgt = parTrgt;

            // check if t_xref element is already present
            string query = @"SELECT XrefID As XREF_ID, description As DESCR
                            FROM  t_object  o inner JOIN t_xref x on (o.ea_guid = x.client)
                            where x.Name = 'CustomProperties' AND
                                  x.Type = 'element property' AND
                                  o.object_ID = " + _parTrgt.ElementID;
                        
                            
            string str = _rep.SQLQuery(query);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(str);

            // get existing t_xref and remember GUID/XrefID
            XmlNode xrefGuid = xmlDoc.SelectSingleNode("//XREF_ID");
            if (xrefGuid != null)
            {
                _xrefid = xrefGuid.InnerText;// GUID of xref

                // get description
                XmlNode xrefDesc = xmlDoc.SelectSingleNode("//DESCR");
                _properties = null;
                if (xrefDesc != null) _properties = xrefDesc.InnerText;
            }
        }
            //---------------------------------------------------------------------
            // setParameterProperties
            //---------------------------------------------------------------------
            // It's possible to call this function several times. It the accumulates the different properties
            //
            public bool SetParameterProperties(string propertyName, string propertyValue) {
                if (propertyName == "direction")
                {
                    var rx = new Regex(@"@PROP=@NAME=direction@ENDNAME;@TYPE=ParameterDirectionKind@ENDTYPE;@VALU=[^@]+@ENDVALU;@PRMT=@ENDPRMT;@ENDPROP;");
                    Match regMatch = rx.Match(_properties);
                    while (regMatch.Success)
                    {
                        // delete old string
                        _properties = _properties.Replace(regMatch.Value, "");
                        regMatch = regMatch.NextMatch();
                        // add new string
                    }
                    _properties = _properties + "@PROP=@NAME=direction@ENDNAME;@TYPE=ParameterDirectionKind@ENDTYPE;@VALU=" + propertyValue + "@ENDVALU;@PRMT=@ENDPRMT;@ENDPROP;";
                }
                if (propertyName == "constant")
                {
                    var rx = new Regex(@"@PROP=@NAME=isStream@ENDNAME;@TYPE=Boolean@ENDTYPE;@VALU=[^@]+@ENDVALU;@PRMT=@ENDPRMT;@ENDPROP;");
                    Match regMatch = rx.Match(_properties);
                    while (regMatch.Success)
                    {
                        // delete old string
                        _properties = _properties.Replace(regMatch.Value, "");
                        regMatch = regMatch.NextMatch();
                        // add new string
                    }
                    _properties = _properties + "@PROP=@NAME=isStream@ENDNAME;@TYPE=Boolean@ENDTYPE;@VALU=" + propertyValue + "@ENDVALU;@PRMT=@ENDPRMT;@ENDPROP;";
                }
        
                return true;
            }
            //---------------------------------------------------------------------
            // Save() Save ParameterProperties to t_xref
            //---------------------------------------------------------------------
            //
            public bool Save()
            {
                // create new entry in t_xref
                if (_xrefid == "")
                {
                    Guid g = Guid.NewGuid();
                    _xrefid = g.ToString();
                    string insertIntoTXref = @"insert into t_xref 
                (XrefID,            Name,               Type,              Visibility, Namespace, Requirement, [Constraint], Behavior, Partition, Description, Client, Supplier, Link)
                VALUES('" + g + "', 'CustomProperties', 'element property','Public', '','','', '',0, '" + _properties + "', '" + _parTrgt.ElementGUID + "', null,'')";
                    _rep.Execute(insertIntoTXref);
                }
                // update propertyValue
                string update = @"update t_xref set description = '" + _properties +
                                "' where XrefID = '" + _xrefid + "'";
                _rep.Execute(update);

                return true;
            }
    }
}
