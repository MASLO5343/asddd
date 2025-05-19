// WpfApp1/Messages/UserMessages.cs
using CommunityToolkit.Mvvm.Messaging.Messages;
using WpfApp1.Models;

namespace WpfApp1.Messages
{
    public class UserAddedMessage : ValueChangedMessage<User>
    {
        public UserAddedMessage(User user) : base(user) { }
    }

    public class UserUpdatedMessage : ValueChangedMessage<User>
    {
        public UserUpdatedMessage(User user) : base(user) { }
    }
}