using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Identifiers;
using JetBlack.Bloomberg.Responses;

namespace JetBlack.Bloomberg.Managers
{
    internal class UserEntitlementsManager : RequestResponseManager<UserEntitlementsRequest, UserEntitlementsResponse, object>, IUserEntitlementsProvider
    {
        public UserEntitlementsManager(Session session, Service service, Identity identity)
            : base(session, service, identity)
        {
        }

        public override IObservable<UserEntitlementsResponse> ToObservable(UserEntitlementsRequest userEntitlementsRequest)
        {
            return Observable.Create<UserEntitlementsResponse>(observer =>
            {
                var request = Service.CreateRequest(OperationNames.UserEntitlementsRequest);
                var userInfoElement = request.GetElement(ElementNames.UserInfo);
                userInfoElement.SetElement(ElementNames.Uuid, userEntitlementsRequest.Uuid);

                var correlationId = new CorrelationID();
                Add(correlationId, observer);

                Session.SendRequest(request, Identity, correlationId);

                return Disposable.Create(() => Session.Cancel(correlationId));
            });
        }

        public override bool CanProcessResponse(Message message)
        {
            return MessageTypeNames.UserEntitlementsResponse.Equals(message.MessageType);
        }

        public override void ProcessResponse(Session session, Message message, bool isPartialResponse, Action<Session, Message, Exception> onFailure)
        {
            IObserver<UserEntitlementsResponse> observer;
            if (!TryGet(message.CorrelationID, out observer))
            {
                onFailure(session, message, new ApplicationException("Failed to find handler"));
                return;
            }

            if (MessageTypeNames.UserEntitlementsResponse.Equals(message.MessageType))
            {
                var entitlementIds = new List<int>();
                var eidDataArrayElement = message.GetElement("eids");
                for (var i = 0; i < eidDataArrayElement.NumValues; ++i)
                    entitlementIds.Add(eidDataArrayElement.GetValueAsInt32(i));

                observer.OnNext(new UserEntitlementsResponse(entitlementIds));

                if (!isPartialResponse)
                {
                    observer.OnCompleted();
                    Remove(message.CorrelationID);
                }
            }
            else
                onFailure(session, message, new Exception("Unknown message type: " + message));
        }
    }
}
