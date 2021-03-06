﻿namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    using TomsToolbox.ObservableCollections;

    [DataContract]
    public class CodeReferenceConfigurationItem : INotifyPropertyChanged
    {
        private string _extensions;
        private bool _isCaseSensitive;
        private string _expression;
        private string _singleLineComment;

        [DataMember]
        public string Extensions
        {
            get
            {
                return _extensions;
            }
            set
            {
                SetProperty(ref _extensions, value, nameof(Extensions));
            }
        }

        [DataMember]
        public bool IsCaseSensitive
        {
            get
            {
                return _isCaseSensitive;
            }
            set
            {
                SetProperty(ref _isCaseSensitive, value, nameof(IsCaseSensitive));
            }
        }

        [DataMember]
        public string Expression
        {
            get
            {
                return _expression;
            }
            set
            {
                SetProperty(ref _expression, value, nameof(Expression));
            }
        }

        [DataMember]
        public string SingleLineComment
        {
            get
            {
                return _singleLineComment;
            }
            set
            {
                SetProperty(ref _singleLineComment, value, nameof(SingleLineComment));
            }
        }

        public IEnumerable<string> ParseExtensions()
        {
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            if (string.IsNullOrEmpty(Extensions))
                return Enumerable.Empty<string>();

            return Extensions.Split(',')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrEmpty(ext));
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void SetProperty<T>(ref T backingField, T value, string propertyName)
        {
            Contract.Requires(!string.IsNullOrEmpty(propertyName));

            if (Equals(backingField, value))
                return;

            backingField = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    [KnownType(typeof(CodeReferenceConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<CodeReferenceConfiguration>))]
    public class CodeReferenceConfiguration
    {
        private ObservableCollection<CodeReferenceConfigurationItem> _items;
        private ObservablePropertyChangeTracker<CodeReferenceConfigurationItem> _changeTracker;

        [DataMember(Name = "Items")]
        public ObservableCollection<CodeReferenceConfigurationItem> Items
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<CodeReferenceConfigurationItem>>() != null);
                CreateCollection();
                return _items;
            }
        }

        public event EventHandler<PropertyChangedEventArgs> ItemPropertyChanged
        {
            add
            {
                CreateCollection();
                _changeTracker.ItemPropertyChanged += value;
            }
            remove
            {
                CreateCollection();
                _changeTracker.ItemPropertyChanged -= value;
            }
        }

        public static CodeReferenceConfiguration Default
        {
            get
            {
                Contract.Ensures(Contract.Result<CodeReferenceConfiguration>() != null);

                var value = new CodeReferenceConfiguration();

                value.Add(".cs,.xaml,.cshtml", true, @"\W($File.$Key)\W", @"//");
                value.Add(".cs", true, @"ResourceManager.GetString\(""($Key)""\)", @"//");
                value.Add(".cs", true, @"typeof\((\w+\.)*($File)\).+""($Key)""|""($Key)"".+typeof\((\w+\.)*($File)\)", @"//");
                value.Add(".vb", false, @"\W($Key)\W", @"'");
                value.Add(".cpp,.c,.hxx,.h", true, @"\W($File::$Key)\W", @"//");
                value.Add(".aspx,.ascx", true, @"<%\$\s+Resources:\s*($File)\s*,\s*($Key)\s*%>", null);
                value.Add(".cs", true, @"StringResourceKey\.($Key)", @"//");

                return value;
            }
        }

        private void Add(string extensions, bool isCaseSensitive, string expression, string singleLineComment)
        {
            Items.Add(
                new CodeReferenceConfigurationItem
                {
                    Extensions = extensions,
                    IsCaseSensitive = isCaseSensitive,
                    Expression = expression,
                    SingleLineComment = singleLineComment
                });
        }

        private void CreateCollection()
        {
            Contract.Ensures(_items != null);
            Contract.Ensures(_changeTracker != null);

            if (_items != null)
                return;

            _items = new ObservableCollection<CodeReferenceConfigurationItem>();
            _changeTracker = new ObservablePropertyChangeTracker<CodeReferenceConfigurationItem>(_items);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant((_items == null) || (_changeTracker != null));
        }
    }
}
