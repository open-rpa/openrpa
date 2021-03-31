/* 
MIT License

Copyright (c) [2016] [Zhou Yang]

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
*/
using SAPFEWSELib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAPBridge
{
    public static partial class Extensions
    {
        public static T FindById<T>(this GuiConnection Connection, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Connection.FindById);
        }

        public static T FindById<T>(this GuiSession Session, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Session.FindById);
        }

        public static T FindById<T>(this GuiFrameWindow FrameWindow, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, FrameWindow.FindById);
        }

        public static T FindById<T>(this GuiApplication Application, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Application.FindById);
        }

        public static T FindById<T>(this GuiMainWindow MainWindow, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, MainWindow.FindById);
        }

        public static T FindById<T>(this GuiModalWindow ModalWindow, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, ModalWindow.FindById);
        }

        public static T FindById<T>(this GuiContainer Container, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Container.FindById);
        }

        public static T FindById<T>(this GuiVContainer VContainer, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, VContainer.FindById);
        }

        public static T FindById<T>(this GuiContextMenu ContextMenu, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, ContextMenu.FindById);
        }

        public static T FindById<T>(this GuiUserArea UserArea, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, UserArea.FindById);
        }

        public static T FindById<T>(this GuiSplitterContainer SplitterContainer, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, SplitterContainer.FindById);
        }

        public static T FindById<T>(this GuiShell Shell, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Shell.FindById);
        }

        public static T FindById<T>(this GuiContainerShell ContainerShell, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, ContainerShell.FindById);
        }

        public static T FindById<T>(this GuiTab Tab, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Tab.FindById);
        }

        public static T FindById<T>(this GuiTabStrip TabStrip, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, TabStrip.FindById);
        }

        public static T FindById<T>(this GuiCustomControl CustomControl, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, CustomControl.FindById);
        }

        public static T FindById<T>(this GuiToolbar Toolbar, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Toolbar.FindById);
        }

        public static T FindById<T>(this GuiMenubar Menubar, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Menubar.FindById);
        }

        public static T FindById<T>(this GuiMenu Menu, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Menu.FindById);
        }

        public static T FindById<T>(this GuiSimpleContainer SimpleContainer, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, SimpleContainer.FindById);
        }

        public static T FindById<T>(this GuiScrollContainer ScrollContainer, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, ScrollContainer.FindById);
        }

        public static T FindById<T>(this GuiStatusbar Statusbar, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Statusbar.FindById);
        }

        public static T FindById<T>(this GuiTableControl TableControl, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, TableControl.FindById);
        }

        public static T FindById<T>(this GuiTitlebar Titlebar, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Titlebar.FindById);
        }

        public static T FindById<T>(this GuiGOSShell GOSShell, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, GOSShell.FindById);
        }

        public static T FindById<T>(this GuiDialogShell DialogShell, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, DialogShell.FindById);
        }

        public static T FindById<T>(this GuiGridView GridView, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, GridView.FindById);
        }

        public static T FindById<T>(this GuiTree Tree, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Tree.FindById);
        }

        public static T FindById<T>(this GuiSplit Split, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Split.FindById);
        }

        public static T FindById<T>(this GuiPicture Picture, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Picture.FindById);
        }

        public static T FindById<T>(this GuiToolbarControl ToolbarControl, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, ToolbarControl.FindById);
        }

        public static T FindById<T>(this GuiTextedit Textedit, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Textedit.FindById);
        }

        public static T FindById<T>(this GuiOfficeIntegration OfficeIntegration, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, OfficeIntegration.FindById);
        }

        public static T FindById<T>(this GuiHTMLViewer HTMLViewer, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, HTMLViewer.FindById);
        }

        public static T FindById<T>(this GuiCalendar Calendar, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Calendar.FindById);
        }

        public static T FindById<T>(this GuiBarChart BarChart, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, BarChart.FindById);
        }

        public static T FindById<T>(this GuiNetChart NetChart, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, NetChart.FindById);
        }

        public static T FindById<T>(this GuiColorSelector ColorSelector, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, ColorSelector.FindById);
        }

        public static T FindById<T>(this GuiSapChart SapChart, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, SapChart.FindById);
        }

        public static T FindById<T>(this GuiChart Chart, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Chart.FindById);
        }

        public static T FindById<T>(this GuiMap Map, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Map.FindById);
        }

        public static T FindById<T>(this GuiStage Stage, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, Stage.FindById);
        }

        public static T FindById<T>(this GuiGraphAdapt GraphAdapt, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, GraphAdapt.FindById);
        }

        public static T FindById<T>(this GuiEAIViewer2D EAIViewer2D, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, EAIViewer2D.FindById);
        }

        public static T FindById<T>(this GuiEAIViewer3D EAIViewer3D, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, EAIViewer3D.FindById);
        }

        public static T FindById<T>(this GuiApoGrid ApoGrid, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, ApoGrid.FindById);
        }

        public static T FindById<T>(this GuiAbapEditor AbapEditor, string Id)
            where T : class
        {
            return findByIdTemplate<T>(Id, AbapEditor.FindById);
        }
        private static T findByIdTemplate<T>(string Id, Func<string, object, GuiComponent> FindById) where T : class
        {
            try
            {
                return FindById(Id, Type.Missing) as T;
            }
            catch
            {
                return null;
            }
        }

        public static T GetSAPComponentById<T>(this GuiSession session, string id) where T : class
        {
            try
            {
                return session.FindById(id) as T;
            }
            catch
            {
                return null;
            }
        }


    }

}
