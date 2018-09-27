export UNAME = $(shell uname)

MAJ_VERSION=1
MIN_VERSION=0
FUL_VERSION=$(MAJ_VERSION).$(MIN_VERSION)

ifeq ($(UNAME), Darwin)

LFLAGS_LIB=-dynamic \
	-dynamiclib \
	-framework Carbon \
	-framework CoreFoundation \
	-framework IOKit \
	-o

CFLAGS_BASE=-c \
	-Wall \
	-Wunused \
	-Wmissing-include-dirs \
	-Werror \
	-g \
	-O0 \
	-fpic \
	-fno-stack-protector \
	-std=c++11 \
	-Wc++11-extensions
	
EXEC_FLAGS=-std=c++11 -ldl -DOSX -Wc++11-extensions -o
	
else

CFLAGS_BASE=-Wall -fPIC -std=c++11 -Wc++11-extensions -c

LFLAGS_LIB=-shared -Wl,-soname

EXEC_FLAGS=-std=c++11 -rdynamic -ldl -Wc++11-extensions -o

endif

OPENCV_INCLUDES=-I/usr/local/include -I/usr/include -I/usr/include/opencv2 -I/usr/local/include/opencv-3.4.2/
OPENCV_LIBS=-L/usr/local/lib -L/usr/lib -lopencv_core -lopencv_highgui -lopencv_imgcodecs

all: gs lens blazed spots recon
	
gs:
	clang++ $(CFLAGS_BASE) gs.cpp
	
ifeq ($(UNAME), Darwin)
	clang++ $(LFLAGS_LIB) libphase++gs.$(FUL_VERSION).dylib gs.o -lfftw3
	ln -sf libphase++gs.$(FUL_VERSION).dylib libphase++gs.dylib
else
	clang++ $(LFLAGS_LIB),libphase++gs.so.$(MAJ_VERSION) -o libphase++gs.so.$(FUL_VERSION) gs.o -lfftw3
	ln -sf libphase++gs.so.$(FUL_VERSION) libphase++gs.so
endif

lens:
	clang++ $(CFLAGS_BASE) lens.cpp
	
ifeq ($(UNAME), Darwin)
	clang++ $(LFLAGS_LIB) libphase++lens.$(FUL_VERSION).dylib lens.o
	ln -sf libphase++lens.$(FUL_VERSION).dylib libphase++lens.dylib
else
	clang++ $(LFLAGS_LIB),libphase++lens.so.$(MAJ_VERSION) -o libphase++lens.so.$(FUL_VERSION) lens.o
	ln -sf libphase++lens.so.$(FUL_VERSION) libphase++lens.so
endif

blazed:
	clang++ $(CFLAGS_BASE) blazed.cpp
	
ifeq ($(UNAME), Darwin)
	clang++ $(LFLAGS_LIB) libphase++blazed.$(FUL_VERSION).dylib blazed.o
	ln -sf libphase++blazed.$(FUL_VERSION).dylib libphase++blazed.dylib
else
	clang++ $(LFLAGS_LIB),libphase++blazed.so.$(MAJ_VERSION) -o libphase++blazed.so.$(FUL_VERSION) blazed.o
	ln -sf libphase++blazed.so.$(FUL_VERSION) libphase++blazed.so
endif

spots:
	clang++ $(CFLAGS_BASE) spots.cpp
	
ifeq ($(UNAME), Darwin)
	clang++ $(LFLAGS_LIB) libphase++spots.$(FUL_VERSION).dylib spots.o
	ln -sf libphase++spots.$(FUL_VERSION).dylib libphase++spots.dylib
else
	clang++ $(LFLAGS_LIB),libphase++spots.so.$(MAJ_VERSION) -o libphase++spots.so.$(FUL_VERSION) spots.o
	ln -sf libphase++spots.so.$(FUL_VERSION) libphase++spots.so
endif

recon:
	clang++ $(CFLAGS_BASE) recon.cpp
	
ifeq ($(UNAME), Darwin)
	clang++ $(LFLAGS_LIB) libphase++recon.$(FUL_VERSION).dylib recon.o -lfftw3
	ln -sf libphase++recon.$(FUL_VERSION).dylib libphase++recon.dylib
else
	clang++ $(LFLAGS_LIB),libphase++recon.so.$(MAJ_VERSION) -o libphase++recon.so.$(FUL_VERSION) recon.o -lfftw3
	ln -sf libphase++recon.so.$(FUL_VERSION) libphase++recon.so
endif

cleanup:
	rm -f *.o
	
install: all cleanup
	
clean:
	rm -f *.o *.exe *.so *.so.* *.dylib
