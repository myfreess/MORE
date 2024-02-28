# 6. GPIO Management

以我的理解，第6章先讲了一些STM32的通识，然后解释了一下为何需要HAL库，借此进入GPIO编程

Standard Peripheral Library -> STCube HAL

before we can start describing HAL features, it is best to give a quick look at how the STM32 peripherals are mapped to logical addresses and how they are represented inside the HAL library.

Every STM32 peripheral is interconnected to the MCU core by several orders of buses

each of these buses is connected to different clock sources

peripherals are mapped to a specific region of the 4GB address space, starting from 0x4000 0000 and lasting up to 0x5FFF FFFF. This region is further divided in several sub-regions, each one mapped to a specific peripheral

The way this space is organized, and hence how peripherals are mapped, is specific of a given STM32 microcontroller.

One of the HAL roles is to abstract from the specific peripheral mapping. This is done by defining several handlers for each peripheral. A handler is nothing more then a C struct, whose references are used to point to real peripheral address.

下面是正式介绍GPIO

Every STM32 microcontroller has a variable number of general programmable I/Os. The exact number depends on:
• The type of package chosen (LQFP48, BGA176, and so on).
• The family of microcontroller (F0, F1, etc.).
• The usage of external crystals for HSE and LSE.

crystal应该是无源晶振

GPIOs are the way an MCU communicates with the external world. Every board uses a variable number of I/Os to drive external peripherals (e.g. an LED) or to exchange data through several types of communication peripherals (UART, USB, SPI, etc.). Every time we need to configure a peripheral that uses MCU pins, we need to configure its corresponding GPIOs using the HAL_GPIO module.

# 7. Interrupts Management

An interrupt is an asynchronous event that causes stopping the execution of the current code on a priority basis (the more important the interrupt is, the higher its priority; this will cause that a lower-priority interrupt is suspended). The code that services the interrupt is called Interrupt Service Routine (ISR)

Interrupts are a source of multiprogramming: the hardware knows about them and it is responsible of saving the current execution context (that is, the stack frame, the current Program Counter and few other things) before switching to the ISR. They are exploited by Real Time Operating Systems to introduce the notion of tasks. Without help by the hardware it is impossible to have a true preemptive system, which allows switching between several execution contexts without irreparably losing the current execution flow.

Interrupts can originate both by the hardware and the software itself. ARM architecture distinguishes between the two types: interrupts originate by the hardware, exceptions by the software.

Cortex-M processors provide a unit dedicated to exceptions management. This is called Nested Vectored Interrupt Controller (NVIC)

# 8. Universal Asynchronous Serial Communications

Nowadays there is a really high number of serial communication protocols and hardware interfaces available in the electronics industry. The most of them are focused on high transmission bandwidths, like the more recent USB 2.0 and 3.0 standards, the Firewire (IEEE 1394) and so on.
