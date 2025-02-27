#import "CortexLibIosEmbeddedConnection.h"

#import <Foundation/Foundation.h>
#include <EmotivCortexLib/CortexLib.h>

bool InitCortexLib() {
    NSLog(@"operatingSystemVersionString %@", [[NSProcessInfo processInfo] operatingSystemVersionString]);
    
    [CortexLib start:^(void){
        NSLog(@"CortexLib iOS started");
        [CortexLibIosEmbeddedConnection shareInstance];

    }];
    return true;
}

void StopCortexLib() {
    [CortexLib stop];
}

void SendRequest(const char* requestJson) {
    NSString *request = [NSString stringWithUTF8String:requestJson];
    [[CortexLibIosEmbeddedConnection shareInstance] sendRequest:request];
}