using Newtonsoft.Json.Linq;
// using Python.Runtime;
using System;
using System.Collections.Generic;

using OpenRPA.Interfaces;

namespace OpenRPA.Script.Activities
{
    public class ParseUtils
    {
        //public static object PyObjectToObjectForObjectArgumentWithGIL(PyObject pyobj)
        //{
        //    try
        //    {
        //        InvokeCode.InitPython();
        //        using (Py.GIL())
        //        {
        //            return PyObjectToObjectForObjectArgument(pyobj);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Warning(ex.ToString());
        //        throw new Exception("Failed for 'PyObjectToObjectForObjectArgument': " + ex.ToString());
        //    }
        //}

        //public static object PyObjectToObjectForObjectArgument(PyObject pyobj)
        //{
        //    object obj;
        //    if (pyobj.IsNone())
        //    {
        //        obj = null;
        //    }
        //    else if (PyString.IsStringType(pyobj))
        //    {
        //        obj = pyobj.ToString();
        //    }
        //    else if (PyNumber.IsNumberType(pyobj) && ("True" == pyobj.ToString() || "False" == pyobj.ToString()))
        //    {
        //        obj = bool.Parse(pyobj.ToString());
        //    }
        //    else if (PyInt.IsIntType(pyobj))
        //    {
        //        obj = int.Parse(pyobj.ToString());
        //    }
        //    else if (PyFloat.IsFloatType(pyobj))
        //    {
        //        obj = float.Parse(pyobj.ToString());
        //    }
        //    else if (PyDict.IsDictType(pyobj))
        //    {
        //        obj = PyDictToJObject(new PyDict(pyobj));
        //    }
        //    else if (PyList.IsListType(pyobj))
        //    {
        //        obj = PyListToJArray(new PyList(pyobj));
        //    }
        //    else
        //    {
        //        obj = Newtonsoft.Json.JsonConvert.DeserializeObject(pyobj.ToString(), typeof(object));
        //    }

        //    return obj;
        //}


        //public static JArray PyListToJArrayWithGIL(PyList list)
        //{
        //    try
        //    {
        //        InvokeCode.InitPython();
        //        using (Py.GIL())
        //        {
        //            return PyListToJArray(list);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Warning(ex.ToString());
        //        throw new Exception("Failed for 'PyListToJArray': " + ex.ToString());
        //    }
        //}

        /// <summary>
        /// Convert PyList to JArray to solve the problem of `True` (or `False`) in python causing conversion failure
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        //public static JArray PyListToJArray(PyList list)
        //{
        //    JArray jarr = new JArray();
        //    foreach (PyObject pyobj in list)
        //    {
        //        JToken value;
        //        if (pyobj.IsNone())
        //        {
        //            value = null;
        //        }
        //        else if (PyString.IsStringType(pyobj))
        //        {
        //            value = pyobj.ToString();
        //        }
        //        else if (PyNumber.IsNumberType(pyobj) && ("True" == pyobj.ToString() || "False" == pyobj.ToString()))
        //        {
        //            value = bool.Parse(pyobj.ToString());
        //        }
        //        else if (PyInt.IsIntType(pyobj))
        //        {
        //            value = int.Parse(pyobj.ToString());
        //        }
        //        else if (PyFloat.IsFloatType(pyobj))
        //        {
        //            value = float.Parse(pyobj.ToString());
        //        }
        //        else if (PyDict.IsDictType(pyobj))
        //        {
        //            value = PyDictToJObject(new PyDict(pyobj));
        //        }
        //        else if (PyList.IsListType(pyobj))
        //        {
        //            value = PyListToJArray(new PyList(pyobj));
        //        }
        //        else
        //        { // ? process other types
        //            value = pyobj.ToString();
        //        }

        //        jarr.Add(value);
        //    }

        //    return jarr;
        //}

        //public static JObject PyDictToJObjectWithGIL(PyDict dict)
        //{
        //    try
        //    {
        //        InvokeCode.InitPython();
        //        using (Py.GIL())
        //        {
        //            return PyDictToJObject(dict);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Warning(ex.ToString());
        //        throw new Exception("Failed for 'PyDictToJObject': " + ex.ToString());
        //    }
        //}


        /// <summary>
        /// Convert PyDict to JObject to solve the problem of `True` (or `False`) in python causing conversion failure
        /// </summary>
        /// <param name="dict"></param>
        //    /// <returns></returns>
        //    public static JObject PyDictToJObject(PyDict dict)
        //    {
        //        JObject jobj = new JObject();
        //        foreach(object _key in dict.Keys())
        //        {
        //            var key = _key.ToString();
        //            PyObject pyobj = dict.GetItem(key);
        //            JToken value;
        //            if (pyobj.IsNone())
        //            {
        //                value = null;
        //            }
        //            else if (PyString.IsStringType(pyobj))
        //            {
        //                value = pyobj.ToString();
        //            }
        //            else if (PyNumber.IsNumberType(pyobj) && ("True" == pyobj.ToString() || "False" == pyobj.ToString()))
        //            {
        //                value = bool.Parse(pyobj.ToString());
        //            }
        //            else if (PyInt.IsIntType(pyobj))
        //            {
        //                value = int.Parse(pyobj.ToString());
        //            }
        //            else if (PyFloat.IsFloatType(pyobj))
        //            {
        //                value = float.Parse(pyobj.ToString());
        //            }
        //            else if (PyDict.IsDictType(pyobj))
        //            {
        //                value = PyDictToJObject(new PyDict(pyobj));
        //            }
        //            else if (PyList.IsListType(pyobj))
        //            {
        //                value = PyListToJArray(new PyList(pyobj));
        //            }
        //            else
        //            { // ? process other types
        //                value = pyobj.ToString();
        //            }

        //            jobj.Add(key.ToString(), value);
        //        }

        //        return jobj;
        //    }

        //    public static PyObject ToPyObjectWithGIL(object obj)
        //    {
        //        try
        //        {
        //            InvokeCode.InitPython();
        //            using (Py.GIL())
        //            {
        //                return ToPyObject(obj);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Warning(ex.ToString());
        //            throw new Exception("Failed for 'ToPyObject': " + ex.ToString());
        //        }
        //    }

        //    /// <summary>
        //    /// Convert objects in c# to PyObject (such as list or dict) to facilitate data access in python
        //    /// </summary>
        //    /// <param name="obj"></param>
        //    /// <returns></returns>
        //    public static PyObject ToPyObject(object obj)
        //    {
        //        if (obj == null)
        //        {
        //            return Runtime.None;
        //        }
        //        else if (obj is JObject)
        //        {
        //            PyDict pyDict = new PyDict();
        //            foreach (var item in (JObject)obj)
        //            {
        //                pyDict.SetItem(item.Key, ToPyObject(item.Value));
        //            }
        //            return pyDict;
        //        }
        //        else if (obj is JArray)
        //        {
        //            PyList pyList = new PyList();
        //            foreach (var item in (JArray)obj)
        //            {
        //                pyList.Append(ToPyObject(item));
        //            }
        //            return pyList;
        //        }
        //        else if (obj is JValue)
        //        {
        //            object value = ((JValue)obj).Value;
        //            return ToPyObject(value);
        //        }
        //        else if (ScriptActivities.IsDictionary(obj, typeof(string)))
        //        {
        //            IDictionary<string, object> dict = (IDictionary<string, object>)obj;
        //            PyDict pyDict = new PyDict();
        //            foreach (var item in dict)
        //            {
        //                pyDict.SetItem(item.Key, ToPyObject(item.Value));
        //            }
        //            return pyDict;
        //        }
        //        else if (ScriptActivities.IsList(obj, null))
        //        {
        //            IList<object> list = (IList<object>)obj;
        //            PyList pyList = new PyList();
        //            foreach (var item in list)
        //            {
        //                pyList.Append(ToPyObject(item));
        //            }
        //            return pyList;
        //        }
        //        else
        //        {
        //            return obj.ToPython();
        //        }
        //    }
        //}
    }
}
