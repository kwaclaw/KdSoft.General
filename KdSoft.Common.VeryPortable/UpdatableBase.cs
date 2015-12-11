using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace KdSoft.Common
{
    /// <summary>
    /// Implementation of <see cref="INotifyPropertyChanged"/> to simplify models.
    /// </summary>
    public abstract class UpdatableBase: INotifyPropertyChanged
    {
        public UpdatableBase() {
            publicProperties = new List<PropertyInfo>();
#if !COREFX
            var props = this.GetType().GetProperties(
              BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
#else
            var props = this.GetType().GetRuntimeProperties();
#endif
            foreach (var prop in props) {
                var getMethod = prop.GetGetMethod();
                var setMethod = prop.GetSetMethod();
                if (getMethod == null || setMethod == null)
                    continue;
                if (getMethod.IsPublic && setMethod.IsPublic)
                    publicProperties.Add(prop);
            }
        }

        UpdatableBase original;
        /// <summary>
        /// Original copy of object.
        /// </summary>
        public UpdatableBase Original {
            get { return original; }
        }

        bool isTracking;
        public bool IsTracking { get { return isTracking; } }

        /// <summary>
        /// Must be called to enable tracking of first change - backup to Original.
        /// This is necessary since the original object creation would create an "empty"Original at the first property assignment.
        /// </summary>
        public void StartTracking() {
            isTracking = true;
        }

        /// <summary>
        /// Reverts to the "unchanged" state, to be used after a Save operation.
        /// </summary>
        public void ResetOriginal() {
            original = null;
            OnPropertyChanged("Original");
        }

        IList<PropertyInfo> publicProperties;
        /// <summary>
        /// Returns <see cref="PropertyInfo"/> objects for all public read/write properties.
        /// </summary>
        protected IList<PropertyInfo> PublicProperties {
            get { return publicProperties; }
        }

        /// <summary>
        /// Resets all public read/write properties to their original value, if applicable.
        /// </summary>
        public void UndoChanges() {
            var tmpOriginal = original;
            if (tmpOriginal == null)
                return;
            bool oldTracking = isTracking;
            isTracking = false;  // turn of tracking changes while resetting the properties
            try {
                foreach (var prop in PublicProperties) {
                    prop.SetValue(this, prop.GetValue(tmpOriginal, null), null);
                }
                ResetOriginal();
            }
            finally {
                isTracking = oldTracking;
            }
        }

        /// <summary>
        /// Resets property specified by name to its original value.
        /// </summary>
        /// <param name="name">Name of property.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public bool UndoProperty(string name) {
            var tmpOriginal = original;
            if (tmpOriginal == null)
                return false;
            var prop = PublicProperties.Where(pi => pi.Name == name).FirstOrDefault();
            if (prop == null)
                return false;
            prop.SetValue(this, prop.GetValue(tmpOriginal, null), null);
            return true;
        }

        /// <summary>
        /// Moves the object to the modified state explicitly. This may sometimes be desirable
        /// even if the object's state has not changed.
        /// </summary>
        public void SetModified() {
            if (original == null) {
                original = Clone();
                OnPropertyChanged("Original");
            }
        }

        /// <summary>
        /// Overide, but call inherited.
        /// </summary>
        /// <returns></returns>
        public virtual UpdatableBase Clone() {
            return (UpdatableBase)this.MemberwiseClone();
        }

        /// <summary>
        /// Multicast event for property change notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Checks if a property already matches a desired value. Validates and sets the property and
        /// notifies listeners when property value has actually changed. Updates value even if validation fails.
        /// Validation errros will be added to ErrorMap.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners.  This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected bool SetProperty<T>(ref T storage, T value, String propertyName) {
            if (object.Equals(storage, value)) return false;

            if (isTracking) {  // don't want to validate on object initialization
                SetModified();
            }
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">Name of the property used to notify listeners.  This
        /// value is optional and can be provided automatically when invoked from compilers
        /// that support <see cref="CallerMemberNameAttribute"/>.</param>
        protected void OnPropertyChanged(string propertyName) {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null) {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
