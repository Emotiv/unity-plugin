#import "CortexLibIosEmbeddedConnection.h"

#import <Foundation/Foundation.h>
#include <EmotivCortexLib/CortexLib.h>

typedef void (*UnityStartedCallback)(void);
static UnityStartedCallback startedCb = NULL;

void RegisterUnityStartedCallback(UnityStartedCallback callback) {
    startedCb = callback;
}

bool InitCortexLib() {
    NSLog(@"operatingSystemVersionString %@", [[NSProcessInfo processInfo] operatingSystemVersionString]);
    
    [CortexLib start:^(void){
        NSLog(@"CortexLib iOS started");
        [CortexLibIosEmbeddedConnection shareInstance];
        if (startedCb != NULL) {
            startedCb();
        }
    }];
    return true;
}

void StopCortexLib() {
    [[CortexLibIosEmbeddedConnection shareInstance] close];
    [CortexLib stop];
}

void SendRequest(const char* requestJson) {
    NSString *request = [NSString stringWithUTF8String:requestJson];
    [[CortexLibIosEmbeddedConnection shareInstance] sendRequest:request];
}