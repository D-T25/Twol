#pragma once
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cstdint>
using byte=uint8_t;
const int HIGH=1,LOW=0;
static uint32_t testMillis=1000;
uint32_t millis(){return testMillis;}
void digitalWrite(int,int){}
char* dtostrf(double d,int width,unsigned int precision,char* out){std::sprintf(out,"%*.*f",width,precision,d);return out;}
char* ultoa(unsigned long v,char* out,int){std::sprintf(out,"%lu",v);return out;}
template<class T>T constrain(T x,T a,T b){return x<a?a:x>b?b:x;}
using std::isfinite;
