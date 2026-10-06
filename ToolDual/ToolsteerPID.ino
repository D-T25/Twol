void calcSteeringPID(void)
{
    //proportional type valve
    if (toolSettings.isDirectionalValve == 0)
    {
        pValue = toolSettings.kP * toolXTE_cm * 0.2;
        pwmDrive = (int16_t)pValue;

        errorAbs = abs(toolXTE_cm);


        int16_t newMax = 0;

        if (errorAbs < toolSettings.lowHighDistance)
        {
            newMax = (errorAbs * lowHighPerCM) + toolSettings.lowPWM;
        }
        else newMax = toolSettings.highPWM;
        
        //limit the pwm drive
        if (pwmDrive > newMax) pwmDrive = newMax;
        if (pwmDrive < -newMax) pwmDrive = -newMax;

        if (dCounter++ > 10)
        {
            float f_kD = (float)(toolSettings.kD) * 0.1;
            dValue = (toolXTE_cm - lastXTE_Error) * 30 * f_kD;
            lastXTE_Error = toolXTE_cm;
            dCounter = 0;
        }

        if (dValue > toolSettings.highPWM) dValue = toolSettings.highPWM;
        if (dValue < -toolSettings.highPWM) dValue = -toolSettings.highPWM;

       /* Serial.print(dValue);
        Serial.print("\t");
        Serial.println(pwmDrive);*/

        //add the derivative value;
        pwmDrive += dValue;  

        if (pwmDrive > toolSettings.highPWM)  pwmDrive = toolSettings.highPWM;
        if (pwmDrive < -toolSettings.highPWM) pwmDrive = -toolSettings.highPWM;
        
        //add min throttle factor so no delay from motor resistance.
        if (pwmDrive < 0) pwmDrive -= toolSettings.minPWM;
        else if (pwmDrive > 0) pwmDrive += toolSettings.minPWM;
    }
    else //Directional valve
    {
        pwmDrive = 0;
        if (bitRead(guidanceStatus, 0) != 0)
        {
            errorAbs = abs(toolXTE_cm);

            if (errorAbs > toolSettings.lowHighDistance)
            {
                if (toolXTE_cm > 0) pwmDrive = 255;
                else pwmDrive = -255;
            }
            else
            {
                if (valveOnCounter < toolSettings.valveOnTime) 
                {
                    pwmDrive = 255;
                }
                else if (valveOffCounter < toolSettings.valveOffTime) 
                {
                    pwmDrive = 0;
                }
                else if (valveOffCounter > toolSettings.valveOffTime)
                {
                    valveOnCounter = 0;
                    valveOffCounter = 0;
                }

                if (toolXTE_cm < 0) pwmDrive *= -1;
            }
        }
    }
}

//#########################################################################################

void motorDrive(void)
{
    // Used with Cytron MD30C Driver
    // Steering Motor
    // Dir + PWM Signal

	//Override the set pwmDrive with manualPWM if not zero
    if (manualPWM != 0)
    {
        pwmDrive = manualPWM;
    }

    if (toolSettings.invertActuator) pwmDrive *= -1;

    if (abs(actuatorPositionPercent) > toolSettings.maxActuatorLimit) 
    {
      if (actuatorPositionPercent > 0 && pwmDrive > 0)
      {
          pwmDrive = 0;
      }
      else if (actuatorPositionPercent < 0 && pwmDrive < 0)
      {
          pwmDrive = 0;
      }
    }

 
    pwmDisplay = pwmDrive;

    if (toolSettings.CytronDriver)
    {
        // Cytron MD30C Driver Dir + PWM Signal
        if (pwmDrive >= 0)
        {
            bitSet(PORTD, 4);  //set the correct direction
        }
        else
        {
            bitClear(PORTD, 4);
            pwmDrive = -1 * pwmDrive;
        }

        //write out the 0 to 255 value
        analogWrite(PWM1_LPWM, pwmDrive);
    }
    else
    {
        // IBT 2 Driver Dir1 connected to BOTH enables
        // PWM Left + PWM Right Signal

        if (pwmDrive > 0)
        {
            analogWrite(PWM2_RPWM, 0);//Turn off before other one on
            analogWrite(PWM1_LPWM, pwmDrive);
        }
        else
        {
            pwmDrive = -1 * pwmDrive;
            analogWrite(PWM1_LPWM, 0);//Turn off before other one on
            analogWrite(PWM2_RPWM, pwmDrive);
        }

        pwmDisplay = pwmDrive;
    }
}
