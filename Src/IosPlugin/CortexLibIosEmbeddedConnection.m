#import "CortexLibIosEmbeddedConnection.h"

static CortexLibIosEmbeddedConnection *sharedInstance = nil;

typedef void (*UnityResponseCallback)(const char* _Nonnull responseMessage);
static UnityResponseCallback unityResponseCb = NULL;

//register callback from unity
void RegisterUnityResponseCallback(UnityResponseCallback callback) {
    unityResponseCb = callback;
}

@implementation CortexLibIosEmbeddedConnection

+(id) shareInstance {
    if(!sharedInstance) {
        sharedInstance = [[CortexLibIosEmbeddedConnection alloc] initAfterCortexStarted];
    }
    return sharedInstance;
}


-(id) initAfterCortexStarted {
    self = [super init];
    if(self) {
        cortexClient = [[CortexClient alloc] init];
        cortexClient.delegate = self;
    }
    return self;
}

-(void)sendRequest:(NSString *)nsJsonString {
    [cortexClient sendRequest:nsJsonString];
}

-(void)close {
    [cortexClient close];
}

-(void)processResponse:(NSString *)responseMessage {
    if (unityResponseCb != NULL) {
        const char* cString = [responseMessage UTF8String];
        unityResponseCb(cString);
    }
}
@end