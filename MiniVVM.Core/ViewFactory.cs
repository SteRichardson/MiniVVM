﻿using System;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiniVVM
{
    public class ViewFactory
    {

        static Lazy<ViewFactory> current =
            new Lazy<ViewFactory>(() => new ViewFactory(), true);

        public static ViewFactory Current
        {
            get
            {
                return current.Value;
            }
        }

        public void RegisterView<TViewModel, TView>(TargetIdiom idiom = TargetIdiom.Phone) 
            where TViewModel : ViewModel 
            where TView : VisualElement
        {
            try
            {
                ViewRegister.ExportedViews.Add(new ExportedView(typeof(TView), typeof(TViewModel), idiom));
            }
            catch (Exception ex)
            {
                
            }
        }

        public Page ResolveView<TViewModel>(Dictionary<string, object> data = null) where TViewModel : ViewModel
        {
            var views = ViewRegister.GetViewsByViewModel<TViewModel>();

            var exportedView = views.Any(v => v.TargetIdom == Device.Idiom)
                ? views.Single(v => v.TargetIdom == Device.Idiom)
                : views.Single(v => v.TargetIdom == TargetIdiom.Phone);

            var view = (Page)Activator.CreateInstance(exportedView.ViewType);
            var viewModel = (ViewModel)Activator.CreateInstance(exportedView.ViewModelType);

            view.BindingContext = viewModel;
            viewModel.Navigation = view.Navigation;
            view.BindingContext = viewModel;

            if (data != null)
                PopulateViewModel(viewModel, data);

            viewModel.Init(data);
            return view;
        }

        void PopulateViewModel(ViewModel viewModel, Dictionary<string, object> data)
        {
            var viewModelType = viewModel.GetType();

            var properties = viewModelType.GetRuntimeProperties().ToList();

            foreach (var key in data.Keys)
            {
                var property = properties.FirstOrDefault(x => x.Name == key);
                if (property == null)
                    continue;

                var typeInfo = property.PropertyType.GetTypeInfo();

                var val = data[key];
                if (val == null)
                {
                    if (typeInfo.IsClass)
                    {
                        property.SetValue(viewModel, null);
                    }
                    else if (typeInfo.IsGenericType && typeof (Nullable<>).GetTypeInfo().IsAssignableFrom(typeInfo))
                    {
                        property.SetValue(viewModel, null);
                    }
                }
                else if (typeInfo.IsAssignableFrom(val.GetType().GetTypeInfo())) 
                {
                    property.SetValue(viewModel, val);
                }
            }
        }
    }
}

