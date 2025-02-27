#import "CortexLibIosEmbeddedConnection.h"

static CortexLibIosEmbeddedConnection *sharedInstance = nil;

typedef void (*UnityResponseCallback)(const char* _Nonnull responseMessage);
static UnityResponseCallback unityCallback = NULL;

//register callback from unity
void RegisterUnityResponseCallback(UnityResponseCallback callback) {
     unityCallback = callback;
}

@implementation CortexLibIosEmbeddedConnection

+(id) shareInstance {
    if(!sharedInstance) {
        sharedInstance = [[CortexLibIosEmbeddedConnection alloc] initAfterCortexStarted];
    }
    return sharedInstance;
}

-(id) init {
    return nil;
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
    // Implement the logic to handle the request here
    [cortexClient sendRequest:nsJsonString];
}

-(void)processResponse:(NSString *)responseMessage {
    if (unityCallback != NULL) {
        const char* cString = [responseMessage UTF8String];
        unityCallback(cString);
    }
}
@end