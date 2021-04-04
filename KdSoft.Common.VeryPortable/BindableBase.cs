using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KdSoft.Common
{
  /// <summary>
  /// Implementation of <see cref="INotifyPropertyChanged"/> to simplify models.
  /// </summary>
  public abstract class BindableBase: INotifyPropertyChanged
  {
    /// <summary>
    /// Multicast event for property change notifications.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Checks if a property already matches a desired value.  Sets the property and
    /// notifies listeners only when necessary.
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
      if (object.Equals(storage, value))
        return false;

      storage = value;
      this.OnPropertyChanged(propertyName);
      return true;
    }

    /// <summary>
    /// Notifies listeners that a property value has changed.
    /// </summary>
    /// <param name="propertyName">Name of the property used to notify listeners.  This
    /// value is optional and can be provided automatically when invoked from compilers
    /// that support <see cref="CallerMemberNameAttribute"/>.</param>
    public void OnPropertyChanged(string propertyName = null) {
      var eventHandler = this.PropertyChanged;
      if (eventHandler != null) {
        eventHandler(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    /// <summary>
    /// Overide, but call inherited.
    /// </summary>
    /// <returns></returns>
    public virtual BindableBase Clone() {
      return (BindableBase)this.MemberwiseClone();
    }

  }
}
